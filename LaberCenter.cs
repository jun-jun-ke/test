using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyTools;

namespace Labeller
{
    internal class LaberCenter
    {
        public static double[] GetLaberRect(HObject image, HWindow window)
        {
            try
            {
                if (image == null) {HOperatorSet.DispText(window,"标签定位失败！图像为空。","window",10,10,"red",null,null); return null; }
                HOperatorSet.DualThreshold(image, out var regions, 150000, 200, 180);
                HOperatorSet.SelectShape(regions, out var selected, "rect2_len1", "and", 0, 600);
                HOperatorSet.SelectShapeStd(selected, out var selectedRegions, "max_area", 70);
                HOperatorSet.FillUp(selectedRegions, out var regionFillUp);
                HOperatorSet.SmallestRectangle2(selectedRegions, out var row, out var column, out var phi, out var length1, out var length2);
                if (row.Length != 1|| length1.D>600) { HOperatorSet.DispText(window, "标签定位失败！没有找到标签或多个标签。", "window", 10, 10, "red", null, null); return null; }
                regionFillUp?.DispObj(window);
                HOperatorSet.GenRectangle2(out var rectangle, row, column, phi, length2 / 2, length2 / 2);
                HOperatorSet.Opening(regionFillUp, rectangle, out var regionOpening);
                HOperatorSet.SmallestRectangle2(regionOpening, out var r, out var c, out var p, out var l1, out var l2);
                if (r.Length != 1|| (l1.D - l2.D) > 30) { HOperatorSet.DispText(window, "标签定位失败！找到多个疑似标签。", "window", 10, 10, "red", null, null); return null; }
                regionOpening?.DispObj(window);
                HalconHelper.ReleaseObj(regions,selected,selectedRegions,regionFillUp,rectangle,regionOpening);
                HOperatorSet.DispText(window, $"标签定位成功！", "window", 10, 10, "black", null, null);
                return new[] { r.D, c.D, p.D, l1.D, l2.D };
            }
            catch (Exception ex)
            {
                HOperatorSet.DispText(window, $"标签定位过程失败！{ex.Message}。", "window", 10, 10, "red", null, null); 
                return null;
            }
        }
    }
}
