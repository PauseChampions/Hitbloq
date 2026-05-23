using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using UnityEngine;

namespace Hitbloq.Utilities
{
	internal static class BSMLCompat
	{
		public static TableView TableView(CustomListTableData list)
		{
			return (TableView) GetMemberValue(list, "TableView", "tableView")!;
		}

		public static List<CustomListTableData.CustomCellInfo> Data(CustomListTableData list)
		{
			return (List<CustomListTableData.CustomCellInfo>) GetMemberValue(list, "Data", "data")!;
		}

		public static void SetIcon(CustomListTableData.CustomCellInfo cellInfo, Sprite sprite)
		{
			SetMemberValue(cellInfo, sprite, "Icon", "icon");
		}

		public static ImageView Background(Backgroundable backgroundable)
		{
			return (ImageView) GetMemberValue(backgroundable, "Background", "background")!;
		}

		public static ImageView ButtonImage(ButtonIconImage buttonIconImage)
		{
			return (ImageView) GetMemberValue(buttonIconImage, "Image", "image")!;
		}

		public static DropdownWithTableView Dropdown(DropDownListSetting dropDownListSetting)
		{
			return (DropdownWithTableView) GetMemberValue(dropDownListSetting, "Dropdown", "dropdown")!;
		}

		public static void SetValues(DropDownListSetting dropDownListSetting, List<object> values)
		{
			SetMemberValue(dropDownListSetting, values, "Values", "values");
		}

		public static void Parse(string content, GameObject parent, object host)
		{
#if HITBLOQ_BS_1_29_1
			BSMLParser.instance.Parse(content, parent, host);
#else
			BSMLParser.Instance.Parse(content, parent, host);
#endif
		}

		public static Task<Sprite> LoadSpriteFromAssemblyAsync(string resourceName)
		{
			var loadAsync = typeof(BeatSaberMarkupLanguage.Utilities).GetMethod("LoadSpriteFromAssemblyAsync", BindingFlags.Public | BindingFlags.Static, null, new[] {typeof(string)}, null);
			if (loadAsync?.Invoke(null, new object[] {resourceName}) is Task<Sprite> task)
			{
				return task;
			}

			var findSprite = typeof(BeatSaberMarkupLanguage.Utilities).GetMethod("FindSpriteInAssembly", BindingFlags.Public | BindingFlags.Static, null, new[] {typeof(string)}, null);
			return Task.FromResult((Sprite) findSprite!.Invoke(null, new object[] {resourceName})!);
		}

		private static object? GetMemberValue(object instance, params string[] names)
		{
			var type = instance.GetType();
			foreach (var name in names)
			{
				var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
				if (property != null)
				{
					return property.GetValue(instance);
				}

				var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
				if (field != null)
				{
					return field.GetValue(instance);
				}
			}

			return null;
		}

		private static object? GetStaticMemberValue(System.Type type, params string[] names)
		{
			foreach (var name in names)
			{
				var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (property != null)
				{
					return property.GetValue(null);
				}

				var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (field != null)
				{
					return field.GetValue(null);
				}
			}

			return null;
		}

		private static void SetMemberValue(object instance, object value, params string[] names)
		{
			var type = instance.GetType();
			foreach (var name in names)
			{
				var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
				if (property != null)
				{
					property.SetValue(instance, value);
					return;
				}

				var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
				if (field != null)
				{
					field.SetValue(instance, value);
					return;
				}
			}
		}
	}
}
