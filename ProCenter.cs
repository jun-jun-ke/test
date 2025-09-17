using HalconDotNet;
using MyTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labeller
{
    internal class ProCenter
    {
        public static double[] GetProRect(HObject image, HWindow window)
        {
            try
            {
                if (image == null) { HOperatorSet.DispText(window, "产品定位失败！图像为空。", "window", 10, 10, "red", null, null); return null; }
                HOperatorSet.DualThreshold(image, out var regions, 350000, 240, 220);
                HOperatorSet.CountObj(regions, out var number);
                HOperatorSet.GenEmptyObj(out var objs);
                for (int i = 0; i < number; i++)
                {
                    HOperatorSet.SelectObj(regions, out var obj, i + 1);
                    HOperatorSet.SmallestRectangle2(obj, out var ro, out var co, out var pi, out var leng1, out var leng2);
                    if ((leng1.D - leng2.D) < 30)
                    {
                        HOperatorSet.ConcatObj(objs, obj, out objs);
                    }
                }
                HOperatorSet.FillUp(objs, out var regionFillUp);
                HOperatorSet.SelectShape(regionFillUp, out var selectedRegion, "rectangularity","and", 0.85,1);
                HOperatorSet.SelectShapeStd(selectedRegion, out var selectedRegions, new[] { "max_area", }, 70);
                HOperatorSet.SmallestRectangle2(selectedRegions, out var row, out var column, out var phi, out var length1, out var length2);
                if (row.Length != 1 || (length1.D - length2.D) > 30) { HOperatorSet.DispText(window, "产品定位失败！没有找到产品或多个产品。", "window", 10, 10, "red", null, null); return null; }
                selectedRegions?.DispObj(window);
                HOperatorSet.GenRectangle2(out var rectangle, row, column, phi, length2 / 2, length2 / 2);
                HOperatorSet.Opening(regionFillUp, rectangle, out var regionOpening);
                HOperatorSet.SelectShapeStd(regionOpening, out regionOpening, new[] { "max_area", }, 70);

                HOperatorSet.SmallestRectangle2(regionOpening, out var r, out var c, out var p, out var l1, out var l2);
                if (r.Length != 1) { HOperatorSet.DispText(window, "产品定位失败！找到多个疑似产品。", "window", 10, 10, "red", null, null); return null; }
                regionOpening?.DispObj(window);
                HalconHelper.ReleaseObj(regions,objs,selectedRegion, selectedRegions, regionFillUp, rectangle, regionOpening);
                HOperatorSet.DispText(window, $"产品定位成功！", "window", 10, 10, "black", null, null);
                return new[] { r.D, c.D, p.D, l1.D, l2.D };
            }
            catch (Exception ex)
            {
                HOperatorSet.DispText(window, $"产品定位过程失败！{ex.Message}。", "window", 10, 10, "red", null, null);
                return null;
            }
        }
    }
}
