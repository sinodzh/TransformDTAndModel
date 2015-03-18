using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransformDTToModel;
using System.Data;
using System.Collections.Generic;

namespace UnitTest
{
    [TestClass]
    public class UnitTestForTransformDTAndModel
    {
        [TestMethod]
        public void TestConvertDataTableToModel()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Id", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Address", typeof(string));
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            dt.Rows.Add("0001", "张三", "武汉市");
            dt.Rows.Add("0002", "李四", "北京市");
            dt.AcceptChanges();
            dt.Rows.Add("0003", "王五", "深圳市");

            List<People> allPeople = new List<People>();

            TransformUtil.ConvertDataTableToModel<People>(dt, allPeople);

            //断言是不是只有一个数据，平且是只是修改状态的王五这个人
            Assert.AreEqual(allPeople.Count, 1);
            Assert.AreEqual(allPeople[0].Name, "王五");
        }

        [TestMethod]
        public void TestConvertModelToDataTable()
        {
            List<People> allPeople = new List<People>()
            {
              new People(){ Id="0001", Name="张三", Address ="武汉市"},
              new People(){ Id="0002", Name="李四", Address ="北京市"},
              new People(){ Id="0003", Name="王五", Address ="深圳市"}
            };

            DataTable dt = TransformUtil.ConvertModelToDataTable<People>(allPeople, null);


            //断言是不是有3行数据，数据的列有3列,第1列是不是Id,第一行第二列是不是张三
            Assert.AreEqual(dt.Rows.Count, 3);
            Assert.AreEqual(dt.Columns.Count, 3);
            Assert.AreEqual(dt.Columns[0].ColumnName, "Id");
            Assert.AreEqual(dt.Rows[0][1], "张三");
        }
    }
}
