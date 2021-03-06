export class EnumConstant {

    static readonly elementTypes = [
        { name: '菜单', value: 1 },
        { name: '按钮', value: 2 },
        { name: '链接', value: 3 },
    ];

    static readonly operateTypes = [
        { name: '相等', value: '1' },
        { name: '不相等', value: '2' },
        { name: '字符串包含', value: '3' },
        { name: '字符串不包含', value: '4' },
        { name: '被列表包含', value: '5' },
        { name: '被列表不包含', value: '6' },
    ]

    static readonly combineTypes = [
        { name: '并且', value: '1' },
        { name: '或者', value: '2' },
    ]

}