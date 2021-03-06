﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Yu.Model.WebAdmin.Group.InputModels;
using Yu.Model.WebAdmin.Group.OutputModels;

namespace Yu.Service.WebAdmin.Group
{
    public interface IGroupService
    {

        /// <summary>
        /// 取得所有组织
        /// </summary>
        IEnumerable<GroupResult> GetAllGroups();

        /// <summary>
        /// 删除组织
        /// </summary>
        Task DeleteGroupAsync(Guid groupId);

        /// <summary>
        /// 创建新组织
        /// </summary>
        Task CreateGroupAsync(GroupDetail groupDetail);

        /// <summary>
        /// 更新组织
        /// </summary>
        Task UpdateGroupAsync(GroupDetail groupDetail);

        /// <summary>
        /// 获取后代子树Id
        /// </summary>
        /// <returns></returns>
        IEnumerable<Guid> GetDescendantGroup(Guid groupId);

        /// <summary>
        /// 获取类型名称
        /// </summary>
        string GetGroupNameById(Guid id);
    }
}
