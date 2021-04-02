﻿using System.Windows.Media.Imaging;

namespace ErogeHelper.Common
{
    public static class StaticXamlUiValues
    {
        public static readonly double ControllerWidth = 240;
        public static readonly double AssistiveTouchSize = 55;
        public static readonly BitmapImage TransparentImage =
            Utils.LoadBitmapFromResource("assets/text_background/transparent.png");
        public static readonly BitmapImage AquaGreenImage =
            Utils.LoadBitmapFromResource("assets/text_background/aqua_green.png");
        public static readonly BitmapImage GreenImage = 
            Utils.LoadBitmapFromResource("assets/text_background/green.png");
        public static readonly BitmapImage PinkImage = 
            Utils.LoadBitmapFromResource("assets/text_background/pink.png");
    }
}