export class WorkflowFlowNode {
    id?:string;          // id

    defineId: string;   // 流程图定义ID
    nodeId: string;     // 节点ID
    nodeType: string;   // 节点类型
    top: string;        // 上边距
    left: string;       // 左边距

    name: string;        // 工作节点的名称
    describe: string;    // 工作节点的描述

    handleType: number = 1;  // 办理方式类型
    handlePeoples: string = '';   // 办理人id
    positionId: string;          // 岗位id
    positionGroup: number = 1;   // 岗位所属部门
}