using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MoreTaxTokensSimple
{
	[BepInPlugin("dyxc666.MoreTaxTokensSimple", "More Tax Tokens Simple", "1.7.1")]
	[BepInProcess("REPO.exe")]
	[BepInDependency("Zichen.CurrencyAsToken", BepInDependency.DependencyFlags.SoftDependency)]
	public class MoreTaxTokensPlugin : BaseUnityPlugin
	{
		internal static ManualLogSource Log;
		public static ConfigEntry<int> TokenMultiplier;
		public static ConfigEntry<bool> ConvertTokensToCurrency;
		public static ConfigEntry<int> MaxCurrencyBonus;

		private readonly Harmony harmony = new Harmony("dyxc666.MoreTaxTokensSimple");

		// 按稀有度汇率 [普通, 稀有, 珍贵, 超稀有]，单位 K
		private static readonly int[] CURRENCY_PER_RARITY_K = { 2, 4, 10, 15 };

		// 防止 ResultScreenUI 循环调用导致重复加钱
		private static bool _currencyAlreadyAddedThisCycle = false;

		private void Awake()
		{
			Log = Logger;

			TokenMultiplier = Config.Bind("General", "TokenMultiplier", 10,
				new ConfigDescription(
					"税款代币获取倍率。每提取一个外观箱子，获得原数量 × 倍率 个代币。",
					new AcceptableValueRange<int>(1, 100)
				));

			MaxCurrencyBonus = Config.Bind("General", "MaxCurrencyBonus", 50000,
				new ConfigDescription(
					"单次折现总金额上限，单位：实际金额（非K）。范围 50000-200000。",
					new AcceptableValueRange<int>(50000, 200000)
				));

			ConvertTokensToCurrency = Config.Bind("General", "ConvertTokensToCurrency", true,
				new ConfigDescription(
					"客机无法同步的代币转为团队资金。\n" +
					"true = 代币全拿 + 多出的代币按稀有度汇率折现（封顶见 MaxCurrencyBonus，联机自动同步，推荐搭配 CurrencyAsToken）\n" +
					"false = 仅代币全拿，不发钱（单人/主机独享）"
				));

			// Patch CosmeticTokenAdd
			try
			{
				var original = AccessTools.Method("MetaManager:CosmeticTokenAdd");
				if (original != null)
				{
					harmony.Patch(original,
						prefix: new HarmonyMethod(typeof(MoreTaxTokensPlugin), nameof(CosmeticTokenAddPrefix)));
					Log.LogInfo("✅ Prefix 补丁成功: MetaManager.CosmeticTokenAdd");
				}
				else
				{
					Log.LogWarning("未能找到 MetaManager.CosmeticTokenAdd 方法");
				}
			}
			catch (Exception ex)
			{
				Log.LogError($"补丁失败: {ex.Message}");
			}

			// Patch RunManager.ChangeLevel 用于重置加钱标记
			try
			{
				var changeLevelMethod = AccessTools.Method(typeof(RunManager), "ChangeLevel");
				if (changeLevelMethod != null)
				{
					harmony.Patch(changeLevelMethod,
						prefix: new HarmonyMethod(typeof(MoreTaxTokensPlugin), nameof(ChangeLevelPrefix)));
					Log.LogInfo("✅ Prefix 补丁成功: RunManager.ChangeLevel");
				}
				else
				{
					Log.LogWarning("未能找到 RunManager.ChangeLevel 方法");
				}
			}
			catch (Exception ex)
			{
				Log.LogError($"ChangeLevel 补丁失败: {ex.Message}");
			}

			Log.LogInfo($"More Tax Tokens Simple v1.7.1 初始化完成！TokenMultiplier = {TokenMultiplier.Value}x, ConvertTokensToCurrency = {ConvertTokensToCurrency.Value}, MaxCurrencyBonus = {MaxCurrencyBonus.Value}");
		}

		/// <summary>
		/// 关卡切换时重置加钱标记，允许下一关重新触发折现
		/// </summary>
		[HarmonyPrefix]
		[HarmonyPatch(typeof(RunManager), "ChangeLevel")]
		public static void ChangeLevelPrefix()
		{
			_currencyAlreadyAddedThisCycle = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch]
		public static bool CosmeticTokenAddPrefix(MetaManager __instance, object _rarity)
		{
			int multiplier = TokenMultiplier.Value;
			if (multiplier <= 1) return true;

			int rarityInt = (int)_rarity;
			bool isHost = SemiFunc.IsMasterClientOrSingleplayer();

			if (isHost)
			{
				try
				{
					var field = AccessTools.Field(typeof(MetaManager), "cosmeticTokens");
					var tokens = field?.GetValue(__instance) as List<int>;
					if (tokens == null) return true;

					// 代币全拿
					for (int i = 0; i < multiplier; i++)
					{
						tokens.Add(rarityInt);
					}

					int extraCount = multiplier - 1;

					// 只在第一次触发时加钱，防止 ResultScreenUI 循环调用叠加
					if (!_currencyAlreadyAddedThisCycle && ConvertTokensToCurrency.Value && extraCount > 0)
					{
						_currencyAlreadyAddedThisCycle = true;

						int rateK = CURRENCY_PER_RARITY_K[rarityInt];
						int bonus = extraCount * rateK * 1000;
						bonus = Math.Min(bonus, MaxCurrencyBonus.Value);

						int beforeCurrency = SemiFunc.StatGetRunCurrency();
						SemiFunc.StatSetRunCurrency(beforeCurrency + bonus);

						Log.LogInfo($"💰 主机倍率 ×{multiplier} | 代币 +{multiplier} | 折现 {extraCount} 个（{(SemiFunc.Rarity)rarityInt}，单价 {rateK}K）+{bonus} | {beforeCurrency} → {beforeCurrency + bonus}");
					}
					else if (_currencyAlreadyAddedThisCycle)
					{
						// 已经加过钱了，只记录代币变化
						Log.LogInfo($"💰 主机倍率 ×{multiplier} | 代币 +{multiplier}（{(SemiFunc.Rarity)rarityInt}）| 本关已折现，跳过");
					}
					else
					{
						Log.LogInfo($"💰 主机倍率 ×{multiplier} | 代币 +{multiplier}（稀有度: {(SemiFunc.Rarity)rarityInt}）");
					}
				}
				catch (Exception ex)
				{
					Log.LogError($"主机倍率处理失败: {ex.Message}");
				}
				return false;
			}

			return true;
		}
	}
}