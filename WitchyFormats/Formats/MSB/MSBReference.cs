using System;

namespace WitchyFormats;

[AttributeUsage(AttributeTargets.Property)]
public class MSBReference : Attribute
{
    public Type ReferenceType;
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class MSBParamReference : Attribute
{
    public string ParamName;
}

[AttributeUsage(AttributeTargets.Property)]
public class MSBEntityReference : Attribute
{
}