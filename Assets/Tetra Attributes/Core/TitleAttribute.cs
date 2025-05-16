using UnityEngine;
using System;

namespace TetraCreations.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class TitleAttribute : PropertyAttribute
    {
        #region Constants
        public const float DefaultLineHeight = 1f;
        public const TitleColor DefaultLineColor = TitleColor.LightGray;
        public const TitleColor DefaultTitleColor = TitleColor.Bright;
        #endregion

        #region Properties
        public string Title { get; private set; }
        public float LineHeight { get; private set; }
        public TitleColor LineColor { get; private set; }
        public TitleColor TitleColor { get; private set; }
        public string LineColorString { get; private set; }
        public string TitleColorString { get; private set; }
        public float Spacing { get; private set; }
        public bool AlignTitleLeft { get; private set; }
        #endregion

        public TitleAttribute(string title = "", TitleColor titleColor = DefaultTitleColor,
            TitleColor lineColor = DefaultLineColor, float lineHeight = DefaultLineHeight, float spacing = 14f,
            bool alignTitleLeft = false)
        {
            Title = title;
            TitleColor = titleColor;
            LineColor = lineColor;
            TitleColorString = ColorUtility.ToHtmlStringRGB(TitleColor.ToColor());
            LineColorString = ColorUtility.ToHtmlStringRGB(LineColor.ToColor());
            LineHeight = Mathf.Max(1f, lineHeight);
            Spacing = spacing;
            AlignTitleLeft = alignTitleLeft;
        }
    }
}