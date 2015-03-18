using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TransformDTToModel
{
    public class TransformUtil
    {
        /// <summary>
        /// 将DB中改动的内容同步到泛型集合中
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">dt源</param>
        /// <param name="destinationArray">目标Model集合</param>
        /// <returns></returns>
        public static bool ConvertDataTableToModel<T>(DataTable source, List<T> destinationArray)
            where T : class
        {
            if (source == null || destinationArray == null || source.PrimaryKey == null || source.PrimaryKey.Count() <= 0)
                return false;

            DataTable dtChange = source.GetChanges();
            if (dtChange == null)
                return false;

            List<string> keys = new List<string>();
            foreach (var item in source.PrimaryKey)
            {
                keys.Add(item.ColumnName);
            }

            return ConvertDataTableToModel(source, destinationArray, keys);
        }

        /// <summary>
        /// 同步table里改动的数据到泛型集合里去（新增，修改，删除）
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="source">dt源</param>
        /// <param name="destinationArray">目标Model集合</param>
        /// <param name="keyColumnArray">主键集合</param>
        /// <returns></returns>
        public static bool ConvertDataTableToModel<T>(DataTable source, List<T> destinationArray, List<string> keyColumnArray) 
            where T : class
        {
            if (source == null || destinationArray == null || source.Rows.Count == 0 || keyColumnArray == null || keyColumnArray.Count == 0)
                return false;

            Type modeType = destinationArray.GetType().GetGenericArguments()[0];//模型类型
            PropertyInfo[] ppInfoArray = modeType.GetProperties();//公共属性集合
            List<PropertyInfo> listPPInfo = ppInfoArray.ToList();//方便查询
            //关键列
            List<PropertyInfo> keyPIArray = listPPInfo.FindAll(x => keyColumnArray.Contains(x.Name));

            List<T> listToDelete = new List<T>();
            //新增的数据
            DataRow[] drAddArray = source.Select("", "", DataViewRowState.Added);

            object objItem = modeType.Assembly.CreateInstance(modeType.FullName);
            foreach (DataRow dr in drAddArray)
            {
                destinationArray.Add((T)objItem);
                foreach (System.Reflection.PropertyInfo pi in listPPInfo)
                {
                    pi.SetValue(destinationArray[destinationArray.Count - 1], dr[pi.Name], null);
                }
            }
            //修改和删除的数据
            DataView dvForOP = new DataView(source);
            dvForOP.RowStateFilter = DataViewRowState.Deleted | DataViewRowState.ModifiedCurrent;

            foreach (DataRowView drv in dvForOP)
            {
                for (int i = 0; i < destinationArray.Count; i++)
                {
                    bool blIsTheRow = true;
                    //找出关键列对应的行
                    foreach (System.Reflection.PropertyInfo pInfo in keyPIArray)
                    {
                        object okey = pInfo.GetValue(destinationArray[i], null);
                        if (okey == null)
                            continue;
                        if (drv[pInfo.Name].ToString() != okey.ToString())
                        {
                            blIsTheRow = false;
                            break;
                        }
                    }
                    if (!blIsTheRow)//非本行
                        continue;
                    //根据行状态同步赋值
                    switch (drv.Row.RowState)
                    {
                        case DataRowState.Modified:
                            {
                                foreach (System.Reflection.PropertyInfo pi in listPPInfo)
                                {
                                    if (keyPIArray.Contains(pi))//主键列不更新
                                        continue;
                                    pi.SetValue(destinationArray[i], drv[pi.Name], null);
                                }
                            } break;
                        case DataRowState.Deleted:
                            {
                                listToDelete.Add(destinationArray[i]);
                            } break;
                    }
                }
            }

            for (int i = 0; i < listToDelete.Count; i++)
            {
                destinationArray.Remove(listToDelete[i]);
            }

            return true;
        }

        /// <summary>
        /// 将视图转换成泛型集合
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="dataView">视图</param>
        /// <param name="model">泛型实例</param>
        /// <returns></returns>
        public static List<T> ConvertDataViewToModel<T>(DataView dataView, T model)
            where T:class
        {
            List<T> listReturn = new List<T>();
            Type modelType = model.GetType();
            DataTable dt = dataView.Table;
            //获取model所有类型
            PropertyInfo[] modelProperties = modelType.GetProperties();

            //遍历所有行，逐行添加对象
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                object obj = modelType.Assembly.CreateInstance(modelType.FullName);
                listReturn.Add((T)obj);
                //遍历model所有属性
                foreach (PropertyInfo pi in modelProperties)
                {
                    //遍历所有列
                    foreach (DataColumn col in dt.Columns)
                    {
                        //如果列数据类型与model的数据类型相同、名称相同
                        if (col.DataType == pi.PropertyType
                            && col.ColumnName == pi.Name)
                        {
                            pi.SetValue(obj, dt.Rows[i][col.ColumnName], null);
                        }
                    }
                }
            }

            return listReturn;
        }

        /// <summary>
        /// 将泛型集合类转换成DataTable
        /// </summary>
        /// <typeparam name="T">集合项类型</typeparam>
        /// <param name="sourceArray">集合</param>
        /// <param name="propertyNameArray">需要返回的列的列名，如需返回所有列，此参数传入null值</param>
        /// <returns>数据集(表)</returns>
        public static DataTable ConvertModelToDataTable<T>(IList<T> sourceArray, params string[] propertyNameArray)
            where T:class
        {
            List<string> propertyNameList = new List<string>();
            if (propertyNameArray != null)
                propertyNameList.AddRange(propertyNameArray);

            DataTable result = new DataTable();
            //获取结构
            Type[] typeArr = sourceArray.GetType().GetGenericArguments();
            if (typeArr.Length == 0)
                return result;

            PropertyInfo[] propertys = typeArr[0].GetProperties();
            foreach (PropertyInfo pi in propertys)
            {
                if (propertyNameList.Count == 0)
                {
                    result.Columns.Add(pi.Name, pi.PropertyType);
                }
                else
                {
                    if (propertyNameList.Contains(pi.Name))
                        result.Columns.Add(pi.Name, pi.PropertyType);
                }
            }
            for (int i = 0; i < sourceArray.Count; i++)
            {
                ArrayList tempList = new ArrayList();
                foreach (PropertyInfo pi in propertys)
                {
                    if (propertyNameList.Count == 0)
                    {
                        object obj = pi.GetValue(sourceArray[i], null);
                        tempList.Add(obj);
                    }
                    else
                    {
                        if (propertyNameList.Contains(pi.Name))
                        {
                            object obj = pi.GetValue(sourceArray[i], null);
                            tempList.Add(obj);
                        }
                    }
                }
                object[] array = tempList.ToArray();
                result.LoadDataRow(array, true);
            }

            return result;
        }

    }
}
