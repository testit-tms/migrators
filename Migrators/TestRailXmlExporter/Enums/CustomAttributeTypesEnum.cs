using System.Runtime.Serialization;

namespace TestRailXmlExporter.Enums;

public enum CustomAttributeTypesEnum
{
    [EnumMember(Value = "string")]
    String,

    [EnumMember(Value = "datetime")]
    Datetime,

    [EnumMember(Value = "options")]
    Options,

    [EnumMember(Value = "user")]
    User,

    [EnumMember(Value = "multipleOptions")]
    MultipleOptions,

    [EnumMember(Value = "checkbox")]
    CheckBox
}
