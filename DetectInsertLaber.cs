using HalconDotNet;
using MyTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labeller
{
    internal class DetectInsertLaber
    {
        public static bool GetResult(HObject image, HWindow window)
        {
            try
            {
                if (image == null) { HOperatorSet.DispText(window, "标签有无检测失败！图像为空。", "window", 10, 10, "red", null, null); return false; }
                HOperatorSet.DualThreshold(image, out var regions, 50000, 240, 220);
                HOperatorSet.SelectShape(regions, out var selected, "rect2_len1", "and", 0, 600);
                HOperatorSet.SelectShapeStd(selected, out var selectedRegions, "max_area", 70);
                HOperatorSet.SmallestRectangle2(selectedRegions, out var row, out var column, out var phi, out var length1, out var length2);
                if (row.Length > 0 && length1 < 600)
                { 
                    selectedRegions?.DispObj(window);
                    HalconHelper.ReleaseObj(regions, selected,selectedRegions);
                    HOperatorSet.DispText(window, "疑似有标签存在。", "window", 10, 10, "red", null, null);
                    return true;
                }
                HalconHelper.ReleaseObj(regions,selected, selectedRegions);
                HOperatorSet.DispText(window, "无标签存在。", "window", 10, 10, "black", null, null);
                return false;
            }
            catch (Exception ex)
            {
                HOperatorSet.DispText(window, $"标签检测过程失败！{ex.Message}。", "window", 10, 10, "red", null, null);
                return false;
            }
        }

    }
}
