using HMUI;
using IPA.Utilities;
using UnityEngine.UI;

namespace Hitbloq.Utilities
{
    internal static class Accessors
    {
        public static readonly FieldAccessor<ImageView, bool>.Accessor GradientAccessor = FieldAccessor<ImageView, bool>.GetAccessor("_gradient");
        public static readonly FieldAccessor<ImageView, float>.Accessor SkewAccessor = FieldAccessor<ImageView, float>.GetAccessor("_skew");
        public static readonly FieldAccessor<ModalView, bool>.Accessor AnimateCanvasAccessor = FieldAccessor<ModalView, bool>.GetAccessor("_animateParentCanvas");
        public static readonly FieldAccessor<LevelSelectionFlowCoordinator.State, SelectLevelCategoryViewController.LevelCategory?>.Accessor LevelCategoryAccessor =
            FieldAccessor<LevelSelectionFlowCoordinator.State, SelectLevelCategoryViewController.LevelCategory?>.GetAccessor("levelCategory");
        public static readonly FieldAccessor<TableView, ScrollView>.Accessor ScrollViewAccessor = FieldAccessor<TableView, ScrollView>.GetAccessor("_scrollView");
        public static readonly FieldAccessor<ScrollView, Button>.Accessor PageDownAccessor = FieldAccessor<ScrollView, Button>.GetAccessor("_pageDownButton");
    }
}
