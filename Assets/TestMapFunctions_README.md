# TestMapFunctions 测试脚本使用说明

## 概述

`TestMapFunctions.cs` 是一个独立的测试脚本，用于验证 EasyAR 空间地图编辑器的地图管理功能。该脚本不修改任何现有的管理器，只进行功能测试和验证。

## 功能特性

### 1. 键盘测试功能
- **C键** - 开始创建地图
- **S键** - 保存当前地图
- **L键** - 加载第一个可用地图
- **X键** - 清除当前地图
- **E键** - 进入编辑模式
- **Q键** - 退出编辑模式
- **P键** - 切换点云显示
- **I键** - 显示详细信息

### 2. 自动测试功能
- 可配置的自动测试流程
- 分步骤执行测试
- 自动验证功能状态

### 3. 事件监听
- 监听地图本地化事件
- 监听地图构建事件
- 监听对象放置/移除事件

## 使用方法

### 1. 添加测试脚本
1. 将 `TestMapFunctions.cs` 添加到场景中的任意 GameObject 上
2. 在 Inspector 中配置测试设置

### 2. 配置测试设置
```csharp
[Header("Test Settings")]
public bool enableKeyboardTesting = true;    // 启用键盘测试
public bool enableAutoTesting = false;       // 启用自动测试
public float autoTestInterval = 5f;          // 自动测试间隔

[Header("Debug Info")]
public bool showDebugInfo = true;            // 显示调试信息
```

### 3. 运行测试
1. 启动场景
2. 查看 Console 输出，确认测试脚本正常初始化
3. 使用键盘按键进行功能测试
4. 观察 Console 中的测试结果

## 测试流程

### 步骤1：基础验证
1. 启动场景
2. 检查 Console 是否显示：
   ```
   [TestMapFunctions] 开始测试地图管理功能
   [TestMapFunctions] ✅ EasyARSpatialMapEditorManager 找到
   ```

### 步骤2：地图创建测试
1. 按 **C键** 开始创建地图
2. 检查 Console 是否显示：
   ```
   [TestMapFunctions] 按键C - 开始创建地图
   [EasyAR Spatial Map Editor] 开始构建地图
   ```

### 步骤3：地图保存测试
1. 按 **S键** 保存地图
2. 检查 Console 是否显示：
   ```
   [TestMapFunctions] 按键S - 保存地图
   [EasyAR Spatial Map Editor] 保存地图: GameMap_20231201_143022
   ```

### 步骤4：地图加载测试
1. 按 **L键** 加载地图
2. 检查 Console 是否显示：
   ```
   [TestMapFunctions] 按键L - 加载地图
   [TestMapFunctions] 加载地图: [地图名称]
   ```

### 步骤5：编辑模式测试
1. 按 **E键** 进入编辑模式
2. 按 **Q键** 退出编辑模式
3. 检查 Console 是否显示相应的状态变化

### 步骤6：详细信息查看
1. 按 **I键** 显示详细信息
2. 检查 Console 是否显示完整的编辑器状态信息

## 自动测试模式

### 启用自动测试
1. 在 Inspector 中勾选 `Enable Auto Testing`
2. 设置 `Auto Test Interval`（建议 5-10 秒）
3. 运行场景，观察自动测试流程

### 自动测试步骤
1. **步骤 0**: 开始创建地图
2. **步骤 1**: 等待地图构建完成
3. **步骤 2**: 保存地图
4. **步骤 3**: 加载地图
5. **步骤 4**: 进入编辑模式
6. **步骤 5**: 测试完成，重置到步骤 0

## 事件验证

测试脚本会监听以下事件并输出验证信息：

### 地图事件
- `OnMapBuildingStarted` - 地图构建开始
- `OnMapBuildingCompleted` - 地图构建完成
- `OnMapLocalized` - 地图本地化完成

### 对象事件
- `OnObjectPlaced` - 对象放置
- `OnObjectRemoved` - 对象移除

## 调试信息

### 编辑器状态
测试脚本会定期显示编辑器的完整状态信息：
```
[TestMapFunctions] 编辑器状态:
地图构建: 进行中
地图本地化: 已完成
编辑模式: 开启
对象数量: 3
当前地图: TestMap_20231201_143022
点云数量: 1250
```

### 详细信息
按 **I键** 可查看详细信息，包括：
- 地图构建状态
- 地图本地化状态
- 编辑模式状态
- 对象数量
- 可用地图列表
- 已放置对象列表

## 故障排除

### 常见问题

1. **EasyARSpatialMapEditorManager 未找到**
   - 确保场景中有 EasyARSpatialMapEditorManager 组件
   - 检查组件是否正确初始化

2. **地图构建失败**
   - 检查 EasyAR 组件配置
   - 确保设备支持 AR 功能

3. **对象放置失败**
   - 确保地图已本地化
   - 检查对象预制体配置

### 调试建议

1. 启用 `Show Debug Info` 查看实时状态
2. 使用 `Show Detailed Info` 查看完整信息
3. 检查 Console 中的错误信息
4. 验证 EasyAR 组件配置

## 注意事项

1. **独立性**: 该测试脚本完全独立，不会修改任何现有管理器
2. **安全性**: 测试过程中不会删除或修改重要数据
3. **可配置性**: 所有测试功能都可以通过 Inspector 配置
4. **可扩展性**: 可以根据需要添加更多测试功能

## 扩展功能

可以根据需要添加以下功能：
- 性能测试
- 压力测试
- 边界条件测试
- 错误处理测试
- UI 交互测试 