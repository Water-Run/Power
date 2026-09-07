# 上游参考锁定

测绘日期：2026-08-27。

本目录中的两个 Git checkout 只用于阅读、思想对照和研究，不会作为 Power! 的构建依赖，也不会被父仓库提交。Power! 是 C + Lua 的完整重写，不以源码移植或行为兼容为目标；这样也能把新实现与旧 C++ 应用、旧平台依赖及其子模块许可边界分开。

| 本地目录 | 上游 | 锁定提交 | 描述 |
|---|---|---|---|
| `engine-sim/` | `https://github.com/ange-yaghi/engine-sim.git` | `85f7c3b959a908ed5232ede4f1a4ac7eafe6b630` (`v0.1.11a-7-g85f7c3b`) | 经典开源源码；主仓库为 MIT 许可；包含 5 个子模块 |
| `engine-sim-community-edition/` | `https://github.com/Engine-Simulator/engine-sim-community-edition.git` | `4e5c20da3e2c8b373ec795931b081f4e614048c3` (`v0.1.14a-3-g4e5c20d`) | 发布与教程仓库，没有应用源码；README 明确说明该版本已停止维护 |

经典仓库的锁定提交日期为 2023-01-22；社区仓库的锁定提交日期为 2025-09-11。社区仓库没有源码；经典仓库也只是思想和问题空间的测绘对象，不是移植基线。

## 重新拉取

从项目根目录执行：

```bash
git clone --recurse-submodules https://github.com/ange-yaghi/engine-sim.git reference/engine-sim
git -C reference/engine-sim checkout 85f7c3b959a908ed5232ede4f1a4ac7eafe6b630

git clone https://github.com/Engine-Simulator/engine-sim-community-edition.git \
  reference/engine-sim-community-edition
git -C reference/engine-sim-community-edition checkout \
  4e5c20da3e2c8b373ec795931b081f4e614048c3
```

更新测绘基线前，应先重新运行代码清单、测试清单和架构差异检查，再修改这里的提交号；不要无记录地跟随 `master`。

## 许可边界

- 经典主仓库的 `LICENSE` 是 MIT；如果未来例外复用其受版权保护的代码，必须保留许可和版权声明并登记来源。
- 子模块各有独立许可状态；当前测绘发现 `csv-io`、`delta-studio`、`simple-2d-constraint-solver` 带 MIT 文件，而 `piranha` 与 `direct-to-video` 仍需单独法务确认。
- Power! 采用完整重写策略，不链接旧仓库或旧子模块，也不把旧行为兼容作为验收目标。
- Original Power! material in this archive is now covered by the repository's [licensing notice](../../../COPYING.NOTICE): GPL-3.0-or-later with the [Unity Linking Exception](../../../UNITY-LINKING-EXCEPTION.md). The upstream references retain their original licenses.
