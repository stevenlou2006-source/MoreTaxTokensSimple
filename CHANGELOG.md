# 更新日志

## [1.7.1] - 2026-05-25
### 修复
- 修复团队资金异常叠加的严重 bug：`ResultScreenUI.StateFadeOut` 循环调用 `CosmeticTokenAdd` 导致折现逻辑被重复触发
- 新增 `_currencyAlreadyAddedThisCycle` 标记，同一关卡内只折现一次
- 新增 `RunManager.ChangeLevel` 补丁，关卡切换时自动重置折现标记
- 修复后 10 倍稀有外观箱正常折现 50000，不再出现 5000 万异常

## [1.7.0] - 2026-05-22
### 新增
- 联机同步方案：客机无法同步的代币折成团队资金（游戏原生同步）
- 折现规则：越稀有越值钱（普通2K/稀有4K/珍贵10K/超稀有15K），仅可修改封顶
- `ConvertTokensToCurrency` 开关，关闭后退回纯代币模式
- `MaxCurrencyBonus` 配置项，可自定义封顶金额（范围 50000-200000，默认 50000）
- 软依赖 CurrencyAsToken，联机闭环

### 移除
- EnableMultiplayerSync、ShowStartupPopup 等已废弃配置项


## [1.6.0] - 2026-05-19
### 新增
- 软依赖检测 `CurrencyAsToken`（Zichen 作者），若未安装则自动关闭多人同步
- 新增 `EnableMultiplayerSync` 配置项，控制多人模式下是否全员生效

### 修复
- 修复客机未装 CurrencyAsToken 且同步开关误开时的数据不一致问题

### 说明
- 为什么税款代币无法完美多人同步？
  - REPO 的 `MetaManager.cosmeticTokens` 是一个本地 `List<int>`，Photon 不会自动同步这个列表
  - 经 dnSpy 逆向确认 `MetaManager.Save()` 仅做本地存档，不涉及网络传输
  - v1.1.0 ~ v1.5.3 期间尝试过 RPC、房间属性同步、RaiseEvent、PunRPC 等方案，均因场景切换 ViewID 冲突或事件丢失而失败
  - 最终方案：房主权威 + 客机走原版，如需全员同步请搭配 Zichen 的 `CurrencyAsToken` 模组

## [1.5.3] - 2026-05-19
### 重构
- 参照 `RandomValueMod` 的 RPC 架构，中继器挂载 `PunManager`，跨场景不销毁
- 客机收到 RPC 后清空列表并重加，确保数量与房主完全一致
- 完全解耦客机原版执行状态，不再依赖 `CosmeticTokenAdd` 的执行顺序

## [1.5.2] - 2026-05-19
### 尝试
- 改用 PunRPC 方案，手动创建 PhotonView 固定 ViewID
- 联机测试失败：场景切换导致 ViewID 冲突，客机收不到 RPC

## [1.5.1] - 2026-05-17
### 修复
- 修复主机与客机代币数量不一致的问题

## [1.5.0] - 2026-05-16
### 重构
- 回退到 Prefix 方案，加入 `SemiFunc.IsMasterClientOrSingleplayer()` 严格限定主机执行

### 修复
- 修复 v1.4.2 中主机客机数据叠加导致代币数量随机不一致的问题

## [1.4.3] - 2026-05-15
### 文档
- 修正文本说明

## [1.4.2] - 2026-05-12
### 适配
- 适配游戏 v0.4.2

## [1.4.1] - 2026-05-11
### 修复
- 修复循环逻辑错误：现在正确加满配置的倍率数量（之前少加了一个）
- TokenMultiplier 配置项改为整数类型（ConfigEntry<int>），REPOConfig 中显示为整数滑块

### 变更
- 移除"只需房主安装"的结论。联机时房主和所有客机都必须安装此模组
- 事件码从 180 改为 1，避开原版 200+ 区域的冲突

## [1.4.0] - 2026-05-11
### 新增
- 采用 Photon RaiseEvent（事件码 1）实现房主到客机的代币倍率同步
- IOnEventCallback 接口实现，延迟注册回调避免主菜单卡死

### 变更
- 彻底放弃"Photon 自动同步 List<int>"的错误假设
- 经 dnSpy 逆向确认 MetaManager.Save() 仅做本地存档，无网络同步

## [1.3.0] - 2026-05-11
### 尝试
- 首次尝试自定义事件同步（事件码 180），因与 Omniscye 交流后确认需手动同步

## [1.2.0] - 2026-05-10
### 终止
- 终止网络同步方案的尝试。经深度逆向分析确认 v0.4.0 的代币结算流程为房主权威模式，但 Photon 不会自动同步 cosmeticTokens 列表
- 此版本的"只需房主安装"结论后被证实错误

## [1.1.0 - 1.1.3] - 2026-05-10
### 尝试
- 多次尝试 RPC、房间属性同步等方案，均失败

## [1.0.0] - 2026-05-07
### 正式发布
- 适配游戏 v0.4.0（Cosmetics Update）
- TokenMultiplier 配置项，默认 10x
- 软兼容 REPOConfig

## [0.1.0] - 2026-05-06
### 初始发布（早期版本）
- 使用反射批量探测税款代币结算方法