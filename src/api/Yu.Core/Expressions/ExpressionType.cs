﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Yu.Core.Expressions
{
    /// <summary>
    /// 表达式操作类型
    /// </summary>
    public enum ExpressionType
    {
        Equal = 1, // 相等
        NotEqual = 2, // 不相等
        StringContain = 3, // 字符串包含
        StringNotContain = 4, // 字符串不包含
        ListContain = 5, // list包含
        ListNotContain = 6, // list包含

        // todo 添加更多
    }
}
