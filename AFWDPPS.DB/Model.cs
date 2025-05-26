using SqlSugar;
using System;

namespace AFWDPPS.DB
{
    [SugarIndex("unique_lujing", nameof(序号), OrderByType.Asc, true)]
    public class 稳定平台数据
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int 序号 { get; set; }
        [SugarColumn(IsNullable = true)]
        public string 流水号 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 船俯仰角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 声呐俯仰角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 俯仰电机动作角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 船横滚角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 声呐横滚角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public double 横滚电机动作角度 { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime 时间 { get; set; }
    }
}
