﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Serialize.Linq.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Yu.Core.Constants;
using Yu.Core.Expressions;
using Yu.Core.Utils;
using Yu.Data.Entities.Right;
using Yu.Data.Infrasturctures;
using Yu.Data.Repositories;
using Yu.Model.WebAdmin.Rule.OutputModels;
using ExpressionType = Yu.Core.Expressions.ExpressionType;
using RuleEntity = Yu.Data.Entities.Right.Rule;

namespace Yu.Service.WebAdmin.Rule
{
    public class RuleService : IRuleService
    {
        private readonly IRepository<RuleEntity, Guid> _ruleRepository;

        private readonly IRepository<RuleCondition, Guid> _ruleConditionRepository;

        private readonly IRepository<RuleGroup, Guid> _ruleGroupRepository;

        private readonly IRepository<GroupTree, Guid> _groupTreeRepository;

        private readonly IUnitOfWork<BaseIdentityDbContext> _unitOfWork;

        private readonly UserManager<BaseIdentityUser> _userManager;

        private readonly RoleManager<BaseIdentityRole> _roleManager;

        private readonly IMemoryCache _memoryCache;

        private readonly IHttpContextAccessor _httpContextAccessor;


        public RuleService(IRepository<RuleEntity, Guid> ruleEntityRepository,
            IRepository<RuleCondition, Guid> ruleConditionRepository,
            IRepository<RuleGroup, Guid> ruleGroupRepository,
            IRepository<GroupTree, Guid> groupTreeRepository,
            IUnitOfWork<BaseIdentityDbContext> unitOfWork,
            UserManager<BaseIdentityUser> userManager,
            RoleManager<BaseIdentityRole> roleManager,
            IMemoryCache memoryCache,
            IHttpContextAccessor httpContextAccessor)
        {
            _ruleRepository = ruleEntityRepository;
            _ruleConditionRepository = ruleConditionRepository;
            _ruleGroupRepository = ruleGroupRepository;
            _groupTreeRepository = groupTreeRepository;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _roleManager = roleManager;
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 修改规则组
        /// </summary>
        /// <param name="rules">规则</param>
        /// <param name="ruleConditions">条件</param>
        /// <param name="ruleGroup">规则组</param>
        public async Task<bool> AddOrUpdateRule(IEnumerable<RuleEntityResult> rules, IEnumerable<RuleConditionResult> ruleConditions, RuleGroup ruleGroup)
        {
            // 因为有动态参数的存在,表达式暂时不持久化到数据库,暂时删除group的lambda字段
            //// 生成表达式保存到数据库
            //// 获取实体类型
            var topRule = rules.Where(rule => string.IsNullOrEmpty(rule.UpRuleId)).FirstOrDefault();
            var entityType = EntityTypeFinder.FindEntityType(ruleGroup.DbContext, ruleGroup.Entity);
            var expressionGroup = new ExpressionGroup(entityType);
            var keyValuePairs = new Dictionary<string, string> { };
            keyValuePairs.Add("UserName", _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.UserName).Value);
            keyValuePairs.Add("GroupId", _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.Group).Value ?? string.Empty);
            MakeExpressionGroup(topRule, rules, ruleConditions, entityType, keyValuePairs, ref expressionGroup);

            // 用当前用户数据检查表达式是否正确
            try
            {
                expressionGroup.GetLambda();
            }
            catch
            {
                return false;
            }

            // 开始更新数据
            var group = _ruleGroupRepository.GetByWhereNoTracking(rg => rg.Id == ruleGroup.Id).FirstOrDefault();

            // 已经存在时先删除再插入
            if (group != null)
            {
                // 先删除再插入
                _ruleRepository.DeleteRange(r => r.RuleGroupId == group.Id);
                _ruleConditionRepository.DeleteRange(r => r.RuleGroupId == group.Id);
            }

            // 修改规则组Id
            var groupId = group != null ? group.Id : GuidUtil.NewSquentialGuid();
            ruleGroup.Id = groupId;

            // 修改每项规则的Id和Upid
            foreach (var rule in rules)
            {
                var oldId = rule.Id;

                // 替换前端生成的ID
                var ruleId = GuidUtil.NewSquentialGuid().ToString();
                rule.Id = ruleId;
                rule.RuleGroupId = groupId.ToString();
                foreach (var r in rules)
                {
                    if (r.UpRuleId == oldId)
                    {
                        r.UpRuleId = ruleId;
                    }
                };
                foreach (var c in ruleConditions)
                {
                    if (c.RuleId == oldId)
                    {
                        c.RuleId = ruleId;
                        c.RuleGroupId = groupId.ToString();
                    }
                };
            }

            // 生成新的Id
            ruleConditions.ToList().ForEach(condition => condition.Id = GuidUtil.NewSquentialGuid().ToString());

            // 保存全部数据
            await _ruleRepository.InsertRangeAsync(Mapper.Map<IEnumerable<RuleEntity>>(rules));
            await _ruleConditionRepository.InsertRangeAsync(Mapper.Map<IEnumerable<RuleCondition>>(ruleConditions));

            // 更新或添加规则组
            if (group == null)
            {
                await _ruleGroupRepository.InsertAsync(ruleGroup);
            }
            else
            {
                _ruleGroupRepository.Update(ruleGroup);
            }

            // 提交事务
            await _unitOfWork.CommitAsync();

            return true;
        }

        /// <summary>
        /// 删除规则组
        /// </summary>
        /// <param name="ruleGroupId">规则组ID</param>
        public async Task DeleteRuleGroup(Guid ruleGroupId)
        {
            _ruleGroupRepository.DeleteRange(r => r.Id == ruleGroupId);
            _ruleRepository.DeleteRange(r => r.RuleGroupId == ruleGroupId);
            _ruleConditionRepository.DeleteRange(r => r.RuleGroupId == ruleGroupId);

            // 提交事务
            await _unitOfWork.CommitAsync();
        }

        /// <summary>
        /// 取得所有规则组
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RuleGroup> GetAllRuleGroup()
        {
            return _ruleGroupRepository.GetAllNoTracking();
        }

        /// <summary>
        /// 取得规则组相关数据
        /// </summary>
        /// <param name="ruleGroupId">规则组ID</param>
        /// <returns></returns>
        public RuleResult GetRuleResult(Guid ruleGroupId)
        {
            return new RuleResult
            {
                RuleGroup = _ruleGroupRepository.GetById(ruleGroupId),
                Rules = Mapper.Map<IEnumerable<RuleEntityResult>>(_ruleRepository.GetByWhereNoTracking(rule => rule.RuleGroupId == ruleGroupId)),
                RuleConditions = Mapper.Map<IEnumerable<RuleConditionResult>>(_ruleConditionRepository.GetByWhereNoTracking(condition => condition.RuleGroupId == ruleGroupId))
            };
        }

        /// <summary>
        /// 更新角色拥有的所有数据权限的缓存
        /// </summary>
        public async Task UpdateRulePermissionCache(BaseIdentityUser user)
        {
            var roles = user.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);

            // 清除此用户的缓存
            _memoryCache.Remove(CommonConstants.RuleMemoryCacheKey + user.UserName);

            // 系统管理员可以访问任何数据
            if (roles.Contains(CommonConstants.SystemManagerRole))
            {
                _memoryCache.GetOrCreate(CommonConstants.RuleMemoryCacheKey + user.UserName, entity => new List<string>());
            }
            else
            {
                await _memoryCache.GetOrCreateAsync(CommonConstants.RuleMemoryCacheKey + user.UserName,
                     async entity =>
                     {
                         var ruleGroups = new List<string> { };
                         foreach (var roleName in roles)
                         {
                             // 查找用户所有的规则
                             var role = await _roleManager.FindByNameAsync(roleName);
                             var claims = await _roleManager.GetClaimsAsync(role);

                             // 关联的数据规则
                             var ruleIds = claims.Where(c => c.Type == CustomClaimTypes.Rule).Select(c => c.Value);
                             foreach (var group in _ruleGroupRepository.GetByWhereNoTracking(rg => ruleIds.Contains(rg.Id.ToString())).ToList())
                             {
                                 ruleGroups.Add(group.DbContext + '|' + group.Entity + '|' + GetExpressionStr(group.Id, user.UserName, user.GroupId.ToString()));
                             }
                         }

                         return ruleGroups;
                     });
            }

        }

        /// <summary>
        /// 取得规则组的表达式
        /// </summary>
        /// <param name="ruleGroupId">规则组ID</param>
        /// <returns></returns>
        private string GetExpressionStr(Guid ruleGroupId, string userName, string groupId)
        {
            // 规则信息
            var ruleGroup = _ruleGroupRepository.GetById(ruleGroupId);
            var rules = Mapper.Map<IEnumerable<RuleEntityResult>>(_ruleRepository.GetByWhereNoTracking(rule => rule.RuleGroupId == ruleGroupId));
            var ruleConditions = Mapper.Map<IEnumerable<RuleConditionResult>>(_ruleConditionRepository.GetByWhereNoTracking(condition => condition.RuleGroupId == ruleGroupId));

            // 生成表达式
            var topRule = rules.Where(rule => string.IsNullOrEmpty(rule.UpRuleId)).FirstOrDefault();
            var entityType = EntityTypeFinder.FindEntityType(ruleGroup.DbContext, ruleGroup.Entity);
            var expressionGroup = new ExpressionGroup(entityType);
            var keyValuePairs = new Dictionary<string, string> { };
            keyValuePairs.Add("UserName", userName);
            keyValuePairs.Add("GroupId", groupId);
            MakeExpressionGroup(topRule, rules, ruleConditions, entityType, keyValuePairs, ref expressionGroup);

            // 生成过滤表达式
            Expression lambda = expressionGroup.GetLambda();

            // 表达式序列化
            var serializer = new ExpressionSerializer(new JsonSerializer())
            {
                AutoAddKnownTypesAsListTypes = true
            };
            serializer.AddKnownType(typeof(ExpressionType));
            return serializer.SerializeText(lambda);
        }

        private void MakeExpressionGroup(RuleEntityResult upRule, IEnumerable<RuleEntityResult> rules,
            IEnumerable<RuleConditionResult> ruleConditions, Type entityType, Dictionary<string, string> keyValuePairs,
            ref ExpressionGroup expressionGroup)
        {
            // 查找子规则
            var childRules = from rule in rules
                             where rule.UpRuleId == upRule.Id
                             select rule;

            // 做成子规则
            foreach (var rule in childRules)
            {
                var eg = new ExpressionGroup(entityType) { };
                MakeExpressionGroup(rule, rules, ruleConditions, entityType, keyValuePairs, ref eg);
                expressionGroup.ExpressionGroupsList.Add(eg);
            }

            // 规则类型
            expressionGroup.ExpressionCombineType = (ExpressionCombineType)int.Parse(upRule.CombineType);

            // 规则下的条件
            var conditions = from condition in ruleConditions
                             where condition.RuleId == upRule.Id
                             select condition;

            // 添加条件
            foreach (var condition in conditions)
            {
                if ("${UserName}".Equals(condition.Value))
                {
                    condition.Value = keyValuePairs.GetValueOrDefault("UserName");
                }
                if ("${UserGroupId}".Equals(condition.Value))
                {
                    var groupId = Guid.Parse(keyValuePairs.GetValueOrDefault("GroupId"));
                    if (groupId != new Guid())
                    {
                        var groupTrees = _groupTreeRepository.GetByWhereNoTracking(gt => gt.Ancestor == groupId);
                        var treenodes = groupTrees.Select(g => g.Descendant.ToString()).ToList();
                        expressionGroup.TupleList.Add((condition.Field, treenodes, ExpressionType.ListContain));
                    }
                    continue;
                }
                expressionGroup.TupleList.Add((condition.Field, condition.Value, (ExpressionType)int.Parse(condition.OperateType)));
            }

        }

    }
}
