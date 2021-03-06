﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yu.Data.Entities.Right;
using Yu.Data.Infrasturctures;
using Yu.Model.WebAdmin.Rule.OutputModels;

namespace Yu.Service.WebAdmin.Rule
{
    public interface IRuleService
    {
        /// <summary>
        /// 修改规则组
        /// </summary>
        /// <param name="rules">规则</param>
        /// <param name="ruleConditions">条件</param>
        /// <param name="ruleGroup">规则组</param>
        Task<bool> AddOrUpdateRuleAsync(IEnumerable<RuleEntityResult> rules, IEnumerable<RuleConditionResult> ruleConditions, RuleGroup ruleGroup);

        /// <summary>
        /// 删除规则组
        /// </summary>
        /// <param name="ruleGroupId">规则组ID</param>
        /// <returns></returns>
        Task DeleteRuleGroupAsync(Guid ruleGroupId);

        /// <summary>
        /// 取得所有规则组
        /// </summary>
        /// <returns></returns>
        IEnumerable<RuleGroup> GetAllRuleGroup();

        /// <summary>
        /// 取得规则组相关数据
        /// </summary>
        /// <param name="ruleGroupId">规则组ID</param>
        /// <returns></returns>
        RuleResult GetRuleResult(Guid ruleGroupId);


    }
}
