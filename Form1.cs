using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cameras;
using MyTools;
using Labeller.Properties;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;
using Microsoft.VisualBasic;

namespace Labeller
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            var str = string.Join(",", proTransMatrix);
            var sz = StringHelper.ExtractNums(str).ToArray();
            //List<(string, object, Type)> config = new List<(string, object, Type)>
            // {
            //       ("transMatrix",string.Join(",",transMatrix),typeof(double[])),
            // };
            //DirectoryHelper.CreateDirctory(CalibFile);
            //XMLHelper.SaveXML(CalibFile, config);
        }

        HWindow hWindowBellow, hWindowUp;
        Camera cameraBellow, cameraUp;
        HObject imageBellow, imageUp;
        PlcMcCommunicationHelper plcMc;
        System.Timers.Timer plcTriggerTimer;
        private const string LinkParameterFile = "D:\\Program Files\\Link\\LoadParameter.xml";//连接参数的默认地址
        private bool isWorking;
        double[] searchRegionBellowPoints, searchRegionUpPoints;
        double[] proTransMatrix = new[] { -6.59652e-05, -0.021806, 19.5366, -0.0219335, 2.84589e-05, 10.9746 },
            laberTransMatrix = new[] { -0.0321859, 0.000172922, 163.922, -0.000233911, -0.0323345, 50.4273 };// { -0.0315627, 0.000196338, 163.669, -0.0001512, -0.0316039, 49.5495 };
        double[] laberRect, proRect;
        PointF grabLocation = new PointF((float)123.53, (float)5.0);

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                //设置显示窗口
                InitializeWindow();
                InitializePlc();
                //连接相机
                InitializeCamera();
                //this.InitialLightSource();
                //给相机参数赋初值
                InitializeLinkParameter();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化失败！详情：{ex.Message}");
            }
        }
        private void InitializeWindow()
        {
            InitializeWindow(hSmartWindowControl1, hSmartWindowControl2);
            hWindowBellow = hSmartWindowControl1.HalconWindow;
            hWindowUp = hSmartWindowControl2.HalconWindow;
        }
        private void InitializeWindow(params HSmartWindowControl[] hWindows)
        {
            foreach (var hWindow in hWindows)
            {
                hWindow.MouseWheel += (sender, e) => HalconHelper.ZoomWindow(sender, e);
                HalconHelper.SetDraw(hWindow);
                HalconHelper.SetColor("green", hWindow);
                HOperatorSet.SetLineWidth(hWindow.HalconWindow, 3);
            }
        }
        private void InitializeCamera()
        {
            cameraBellow = new MVSCamera("DA3299333");
            cameraBellow.Open();
            cameraUp = new MVSCamera("00F08453902");
            cameraUp.Open();
        }
        private void InitializePlc()
        {
            //if (txtPLCIP.Text == string.Empty) return;
            //if (!int.TryParse(txtPLCPort.Text, out var port))
            //{
            //    MessageBox.Show($"初始化PLC失败！设置的端口号必须为数字。");
            //    return;
            //}
            plcMc = PlcMcCommunicationHelper.SetPlcMcCommunicationHelper("192.168.3.251", 4988);//192.168.3.251   4988  //txtPLCIP.Text, port
            var R = plcMc.Connect();
            plcTriggerTimer = new System.Timers.Timer
            {
                Interval = 60,
                AutoReset = false
            };
            plcTriggerTimer.Elapsed += PlcTriggerTimer_Elapsed;
        }
        private void InitializeLinkParameter()
        {
            nudUpCameraExposure.Value = 180000;
            nudBellowCameraExposure.Value = 5000;
            return;


            var config = XMLHelper.ReadXML(LinkParameterFile);
            txtLightProt.Text = Convert.ToString(config["lightProt"]);
            txtSerialNumber1.Text = Convert.ToString(config["serialNumber1"]);
            txtSerialNumber2.Text = Convert.ToString(config["serialNumber2"]);
            txtSerialNumber3.Text = Convert.ToString(config["serialNumber3"]);
            txtSerialNumber4.Text = Convert.ToString(config["serialNumber4"]);
            txtPLCIP.Text = Convert.ToString(config["plcIP"]);
            txtPLCPort.Text = Convert.ToString(config["plcPort"]);

            double ConvertDouber(string data)
            {
                if (double.TryParse(data, out var result))
                {
                    return result;
                }
                return double.NaN;
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Settings.Default.BellowCameraExposure = (int)nudBellowCameraExposure.Value;
            //Settings.Default.UpCameraExpousre = (int)nudUpCameraExpousre.Value;

            //Settings.Default.RegionBellowPoints = searchRegionBellowPoints;
            //Settings.Default.RegionUpPoints = searchRegionUpPoints;

            Settings.Default.Save();

            var result = MessageBox.Show("确认要关闭窗体吗？", "提示信息！", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
            try
            {
                plcMc?.Write<bool>("M130", false);
                isWorking = false;
                plcTriggerTimer?.Stop();
                plcMc?.Close();
                CameraClose(cameraBellow, cameraUp);
            }
            catch (Exception)
            {
                // throw;
            }
            void CameraClose(params Camera[] cameras)
            {
                foreach (var camera in cameras)
                {
                    camera?.Close();
                }
            }

        }
        private void btnGrabImage_Click(object sender, EventArgs e)
        {
            //string path1 = PathHelper.SelectPath("read", "bmp文档|*.bmp");

            //if (path1 == string.Empty)
            //{
            //    return;
            //}
            //string path2 = PathHelper.SelectPath("read", "bmp文档|*.bmp");
            //if (path2 == string.Empty)
            //{
            //    return;
            //}
            //HalconHelper.ReleaseObj(imageBellow, imageUp);
            ////HOperatorSet.RotateImage(HalconHelper.ReadImage(path1), out image1, 180, "constant");
            ////HOperatorSet.RotateImage(HalconHelper.ReadImage(path2), out image2, 180, "constant");
            //imageBellow = HalconHelper.ReadImage(path1);
            //imageUp = HalconHelper.ReadImage(path2);
            //DisplayImage(imageBellow, hWindowBellow);
            //DisplayImage(imageUp, hWindowUp);

            //return;
            GrabImage();
        }

        private void cboContinueGrabImage_CheckedChanged(object sender, EventArgs e)
        {
            if (cboContinueGrabImage.Checked)
            {
                Task.Run(() =>
                {
                    while (cboContinueGrabImage.Checked)
                    {
                        GrabImage();
                    }
                });
            }
            btnGrabImage.Enabled = !cboContinueGrabImage.Checked;
        }
        private void GrabImage()
        {
            GrabImage(cameraBellow, ref imageBellow, hWindowBellow, searchRegionBellowPoints, DisplayImage);
            GrabImage(cameraUp, ref imageUp, hWindowUp, searchRegionUpPoints, DisplayImage);
        }
        private void GrabImage(Camera camera, ref HObject image, HWindow hWindow, double[] regionPoints, Action<HObject, HWindow, double[]> action)
        {
            if (camera == null) return;
            image?.Dispose();
            var frame = camera.GetOneFrame(500);
            if (camera == cameraUp)
            {
                frame = camera.GetOneFrame(500);
                frame = camera.GetOneFrame(500);
            }
            image = HalconHelper.ConvertFrameToHImage(frame.ptr, frame.width, frame.height);
            HOperatorSet.SetPart(hWindow, 0, 0, frame.height, frame.width);
            action?.Invoke(image, hWindow, regionPoints);
        }
        private void DisplayImage(HObject image, HWindow hWindow, double[] regionPoints = null)
        {
            HOperatorSet.ClearWindow(hWindow);
            image?.DispObj(hWindow);
            if (regionPoints != null)
                HalconHelper.GenRectangle1(regionPoints).DispObj(hWindow);
        }
        private void btnSaveImage_Click(object sender, EventArgs e)
        {
            //  if (!PreCondition(Precondition.Image)) return;
            SaveImage(new[] { "标签", "产品" }, imageBellow, imageUp);
        }
        private void SaveImage(string[] fileName, params HObject[] images)
        {
            for (int i = 0; i < fileName.Length; i++)
            {
                if (images[i] == null) continue;
                string time = DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss");
                string path = $"D:/手动保存图像/{fileName[i]}/";
                DirectoryHelper.CreateDirctory(path);
                HOperatorSet.WriteImage(images[i], "bmp", 0, $"{path}/{time}.bmp");
                //var image = images[i] as HImage;
                //image.SaveImage("bmp", $"{path}/{time}.bmp");
                //HalconHelper.SaveImage(images[i] as HImage, "bmp", $"{path}/{time}.bmp");
            }
        }
        private void nudUpCameraExposure_ValueChanged(object sender, EventArgs e)
        {
            nudUpCameraExposure.Enabled = false;
            cameraUp.SetParameter("ExposureTime", (float)nudUpCameraExposure.Value);
            nudUpCameraExposure.Enabled = true;
        }

        private void nudBellowCameraExposure_ValueChanged(object sender, EventArgs e)
        {
            nudBellowCameraExposure.Enabled = false;
            cameraBellow?.SetParameter("ExposureTime", (float)nudBellowCameraExposure.Value);
            nudBellowCameraExposure.Enabled = true;
        }

        bool isDrawROI;
        bool startDraw;
        HTuple drawID;

        private void tsbSetLink_Click(object sender, EventArgs e)
        {
            if (Interaction.InputBox("请输入密码", "输入密码", "", 1366 / 2, 768 / 2) == "Admin")
                groupBox1.Visible = true;
        }
        private void butSaveParameter_Click(object sender, EventArgs e)
        {
            List<(string, object, Type)> config = new List<(string, object, Type)>
             {
                   ("lightProt",txtLightProt.Text.Trim(),typeof(string)),
                   ("serialNumber1",txtSerialNumber1.Text.Trim(),typeof(string)),
                   ("serialNumber2",txtSerialNumber2.Text.Trim(),typeof(string)),
                   ("serialNumber3",txtSerialNumber3.Text.Trim(),typeof(string)),
                   ("serialNumber4",txtSerialNumber4.Text.Trim(),typeof(string)),
                   ("plcIP",txtPLCIP .Text.Trim(),typeof(string)),
                   ("plcPort",txtPLCPort .Text.Trim(),typeof(string)),
             };
            DirectoryHelper.CreateDirctory(LinkParameterFile);
            XMLHelper.SaveXML(LinkParameterFile, config);


            // config = new List<(string, object, Type)>
            // {
            //       ("transMatrix",string.Join(",",transMatrix),typeof(double[])),
            // };
            //DirectoryHelper.CreateDirctory(CalibFile);
            //XMLHelper.SaveXML(CalibFile, config);

            groupBox1.Visible = false;
        }
        private void tsbLoadConfig_Click(object sender, EventArgs e)
        {
            string path = PathHelper.SelectPath("read", "xml文档|*.xml");
            if (path == string.Empty)
            {
                return;
            }
            var config = XMLHelper.ReadXML(path);
            nudUpCameraExposure.Value = Convert.ToDecimal(config["UpCameraExposure"]);
            nudBellowCameraExposure.Value = Convert.ToDecimal(config["BellowCameraExposure"]);
            nudOffSetX.Value = Convert.ToDecimal(config["OffSetX"]);
            nudOffSetY.Value = Convert.ToDecimal(config["OffSetY"]);
            nudOffSetR.Value = Convert.ToDecimal(config["OffSetR"]);

            labCurrentProductNumber.Text = $"当前产品料号为：{path}";
        }

        private void tsbSaveConfig_Click(object sender, EventArgs e)
        {
            string path = PathHelper.SelectPath("write", "xml文档|*.xml");
            if (path == string.Empty)
            {
                return;
            }
            List<(string, object, Type)> config = new List<(string, object, Type)>
            {
                ("UpCameraExposure", nudUpCameraExposure.Value.ToString(),typeof(string)),
                ("BellowCameraExposure", nudBellowCameraExposure.Value.ToString(),typeof(string)),
                ("OffSetX", nudOffSetX.Value.ToString(),typeof(string)),
                ("OffSetY", nudOffSetY.Value.ToString(),typeof(string)),
                ("OffSetR",nudOffSetR.Value.ToString(),typeof(string)),
                   };
            //保存料号信息
            XMLHelper.SaveXML(path, config);
            AppendLog($"成功将模板保存。{path}");
        }

        private void tsbStartWork_Click(object sender, EventArgs e)
        {
            isWorking = true;
            plcTriggerTimer.Start();
            cboContinueGrabImage.Checked = false;
            tsbStopWork.Enabled = true;
            EnableControl(false, btnGrabImage, cboContinueGrabImage, tsbStartWork, tsbLoadConfig, tsbSaveConfig);
            plcMc?.Write("M130", true);
        }

        private void tsbStopWork_Click(object sender, EventArgs e)
        {
            isWorking = false;
            plcTriggerTimer.Stop();
            //关闭停止控件
            tsbStopWork.Enabled = false;
            tsbStartWork.Enabled = true;
            EnableControl(true, btnGrabImage, cboContinueGrabImage, tsbStartWork, tsbLoadConfig, tsbSaveConfig);
            plcMc?.Write("M130", false);
        }
        private void EnableControl(bool value, params object[] controls)
        {
            foreach (var control in controls)
            {
                if (control is System.Windows.Forms.Control)
                {
                    (control as System.Windows.Forms.Control).Enabled = value;
                }
                else if (control is ToolStripItem)
                {
                    (control as ToolStripItem).Enabled = value;
                }
            }
        }



        private void DrawRectangle1(HWindow hWindow, HObject image, ref double[] roiPoints, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !startDraw && roiPoints == null)
            {
                startDraw = true;
                drawID = HalconHelper.DrawRectangle1(hWindow, e);
                return;
            }
            if (e.Button == MouseButtons.Right && startDraw)
            {
                var rectPoints = HalconHelper.FinishDrawRectangle1(drawID, hWindow);
                //region?.Dispose();
                //region = HalconHelper.GenRectangle1(rectPoints);
                startDraw = false;
                roiPoints = rectPoints;
                return;
            }
            if (e.Button == MouseButtons.Right && !startDraw)
            {
                hWindow.DispObj(image);
                roiPoints = null;
            }
        }

        private void AppendLog(string msg)
        {
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff");
            string str = $"{time}:{msg}\r\n";
            this.Invoke(new Action(() => txtLog.AppendText(str)));
        }

        private void PlcTriggerTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isWorking) return;
            try
            {
                if (plcMc.Read<bool>("M110", 1).First())//标签定位 xy
                {
                    AppendLog("接收到PLC M110 标签定位信号开始工作");
                    //开始定位标签
                    GrabImage(cameraBellow, ref imageBellow, hWindowBellow, searchRegionBellowPoints, DisplayImage);
                    laberRect = LaberCenter.GetLaberRect(imageBellow, hWindowBellow);
                    if (laberRect != null)
                    {
                        if (proRect != null)
                        {
                            var plcx = plcMc.Read<int>("D3430", 1).First();
                            var plcy = plcMc.Read<int>("D3530", 1).First();
                            grabLocation = new PointF((float)plcx / 100, (float)plcy / 100);
                            //需要知道拍照位
                            var result = CalculateDstPosition(grabLocation, proRect, laberRect, 0);
                            if (result.dstPosition.X < -20 || result.dstPosition.X > 163 || result.dstPosition.Y < -28 || result.dstPosition.Y > 5.3 || Math.Abs(result.dstAngle) > 55)
                            {
                                plcMc?.Write<bool>("M109", true);
                                AppendLog($"设备超行程。 M109已置一。\r\n  X:{result.dstPosition.X}\ty:{result.dstPosition.Y}\tangle{result.dstAngle}");
                                plcMc?.Write<bool>("M110", false);
                                AppendLog("标签定位工作完成 M110 信号已复位");
                                return;
                            }
                            int x = (int)(result.dstPosition.X * 100.0);
                            int y = (int)(result.dstPosition.Y * 100.0);
                            int angle = (int)(result.dstAngle * 100.0);
                            plcMc?.Write<int>("D1516", x);
                            AppendLog($"写入PLC D1516  x：{x}。");
                            plcMc?.Write<int>("D1616", y);
                            AppendLog($"写入PLC D1616  y：{y}。");
                            plcMc?.Write<int>("D1716", angle);
                            AppendLog($"写入PLC D1716  angle：{angle}。");
                            laberRect = null;
                            proRect = null;
                        }
                        plcMc?.Write<bool>("M101", true);
                        AppendLog($"标签定位ok M101已置一。");
                    }
                    else
                    {
                        plcMc?.Write<bool>("M100", true);
                        AppendLog($"标签定位ng M100已置一。");
                    }
                    plcMc?.Write<bool>("M110", false);
                    AppendLog("标签定位工作完成 M110 信号已复位");
                }
                else if (plcMc.Read<bool>("M111", 1).First())////标签定位 angle
                {


                }
                else if (plcMc.Read<bool>("M112", 1).First()) ////标签有无
                {
                    AppendLog("接收到PLC M112 检测标签有无开始工作");
                    GrabImage(cameraBellow, ref imageBellow, hWindowBellow, searchRegionBellowPoints, DisplayImage);
                    var result = !DetectInsertLaber.GetResult(imageBellow, hWindowBellow);
                    if (result)
                    {
                        plcMc?.Write<bool>("M105", true);
                        AppendLog($"检测到没有标签ok M105已置一。");
                    }
                    else
                    {
                        plcMc?.Write<bool>("M104", true);
                        AppendLog($"检测到有标签ng M104已置一。");
                    }
                    plcMc?.Write<bool>("M112", false);
                    AppendLog("检测标签有无工作完成 M112 信号已复位");
                }
                else if (plcMc.Read<bool>("M113", 1).First()) //产品定位
                {
                    AppendLog("接收到PLC M113 产品定位信号开始工作");
                    GrabImage(cameraUp, ref imageUp, hWindowUp, searchRegionUpPoints, DisplayImage);
                    proRect = ProCenter.GetProRect(imageUp, hWindowUp);
                    if (proRect != null)
                    {
                        if (laberRect != null)
                        {
                            var plcx = plcMc.Read<int>("D3430", 1).First();
                            var plcy = plcMc.Read<int>("D3530", 1).First();
                            grabLocation = new PointF((float)plcx / 100, (float)plcy / 100);
                            //需要知道拍照位
                            var result = CalculateDstPosition(grabLocation, proRect, laberRect, 0);
                            if (result.dstPosition.X < -20 || result.dstPosition.X > 163 || result.dstPosition.Y < -28 || result.dstPosition.Y > 5.3 || Math.Abs(result.dstAngle) > 55)
                            {
                                plcMc?.Write<bool>("M109", true);
                                AppendLog($"设备超行程。 M109已置一。\r\n  X:{result.dstPosition.X}\ty:{result.dstPosition.Y}\tangle{result.dstAngle}");
                                plcMc?.Write<bool>("M113", false);
                                AppendLog("产品定位工作完成 M113 信号已复位");
                                return;
                            }
                            int x = (int)(result.dstPosition.X * 100.0);
                            int y = (int)(result.dstPosition.Y * 100.0);
                            int angle = (int)(result.dstAngle * 100.0);
                            plcMc?.Write<int>("D1516", x);
                            AppendLog($"写入PLC D1516  x：{x}。");
                            plcMc?.Write<int>("D1616", y);
                            AppendLog($"写入PLC D1616  y：{y}。");
                            plcMc?.Write<int>("D1716", angle);
                            AppendLog($"写入PLC D1716  angle：{angle}。");
                            laberRect = null;
                            proRect = null;
                        }
                        plcMc?.Write<bool>("M107", true);
                        AppendLog($"产品定位ok M107已置一。");
                    }
                    else
                    {
                        plcMc?.Write<bool>("M106", true);
                        AppendLog($"产品定位ng M106已置一。");
                    }
                    plcMc?.Write<bool>("M113", false);
                    AppendLog("产品定位工作完成 M113 信号已复位");
                }
            }
            catch (Exception ex)
            {
                plcMc?.Write<bool>("M113", false);
                plcMc?.Write<bool>("M112", false);
                plcMc?.Write<bool>("M110", false);
                plcMc?.Write<bool>("M111", false);
                AppendLog($"自动运行检测过程失败！详情：{ex.Message}");
            }
            finally
            {
                if (isWorking)
                    plcTriggerTimer.Start();
            }
        }

        private void butCreatLabelModel_Click(object sender, EventArgs e)
        {
            laberRect = LaberCenter.GetLaberRect(imageBellow, hWindowBellow); ;
            if (laberRect == null || proRect == null) return;
            var result = CalculateDstPosition(grabLocation, proRect, laberRect, 0);
        }

        private void butCreatProModel_Click(object sender, EventArgs e)
        {
            proRect = ProCenter.GetProRect(imageUp, hWindowUp);
            var plcx = plcMc.Read<int>("D3430", 1).First();
            var plcy = plcMc.Read<int>("D3530", 1).First();
            grabLocation = new PointF((float)plcx / 100, (float)plcy / 100);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DetectInsertLaber.GetResult(imageBellow, hWindowBellow);
        }

        //获取贴合位置
        private (PointF dstPosition, double dstAngle) CalculateDstPosition(PointF curPosition, double[] proCenterPx, double[] laberCenterPx, double curAngle)
        {
            //将像素坐标进行转换
            HOperatorSet.AffineTransPoint2d(proTransMatrix, proCenterPx[0], proCenterPx[1], out var proX, out var proY);
            PointF proCenter = new PointF((float)proX.D, (float)proY.D);
            var proLinePx = GetPoints(proCenterPx[0], proCenterPx[1], proCenterPx[2], proCenterPx[3], proCenterPx[4]);
            var proLine = AffineTransPoints(proLinePx, proTransMatrix);

            HOperatorSet.AffineTransPoint2d(laberTransMatrix, laberCenterPx[0], laberCenterPx[1], out var labelX, out var labelY);
            PointF laberCenter = new PointF((float)labelX.D, (float)labelY.D);
            var laberLinePx = GetPoints(laberCenterPx[0], laberCenterPx[1], laberCenterPx[2], laberCenterPx[3], laberCenterPx[4]);
            var laberLine = AffineTransPoints(laberLinePx, laberTransMatrix);

            //计算胶条与凹槽间的偏差角度，单位为弧度(rad)
            double deltaAngle = AngleLines(laberLine, proLine);
            deltaAngle = MathHelper.Deg(deltaAngle) + (double)nudOffSetR.Value;
            if (deltaAngle > 45) deltaAngle -= 90;
            if (deltaAngle < -45) deltaAngle += 90;
            if (deltaAngle > 45) deltaAngle -= 90;
            if (deltaAngle < -45) deltaAngle += 90;

            //机械手旋转，使胶条角度与凹槽角度相同，计算此时胶条的位置
            var rotatePoint = MathHelper.RotatePoint(laberCenter, curPosition, MathHelper.Rad(deltaAngle));
            //计算此时胶条位置与凹槽位置之间的偏差
            double deltaX = proCenter.X - rotatePoint.X;
            double deltaY = proCenter.Y - rotatePoint.Y;
            //机械手当前位置加上此偏差，即为实际要走的位置
            double dstPositionX = curPosition.X + deltaX + (double)nudOffSetX.Value;
            double dstPositionY = curPosition.Y + deltaY + (double)nudOffSetY.Value;
            //机械手当前角度加上偏差角度，即为实际要走的角度
            double dstAngle = curAngle + deltaAngle;
            PointF dstPosition = new PointF((float)dstPositionX, (float)dstPositionY);
            return (dstPosition, dstAngle);
        }

        private static PointF[] GetPoints(HTuple row, HTuple column, HTuple phi, HTuple length1, HTuple length2)
        {
            HOperatorSet.TupleCos(phi, out var cos);
            HOperatorSet.TupleSin(phi, out var sin);
            double a, b, c, d, e, f, g, h;
            a = -length1 * cos - length2 * sin;
            b = -length1 * sin + length2 * cos;
            var point1 = new PointF((float)(row - b).D, (float)(column + a).D);
            c = length1 * cos - length2 * sin;
            d = length1 * sin + length2 * cos;
            var point2 = new PointF((float)(row - d).D, (float)(column + c).D);
            e = length1 * cos + length2 * sin;
            f = length1 * sin - length2 * cos;
            var point3 = new PointF((float)(row - f).D, (float)(column + e).D);
            g = -length1 * cos + length2 * sin;
            h = -length1 * sin - length2 * cos;
            var point4 = new PointF((float)(row - h).D, (float)(column + g).D);
            return new[] { point1, point2 };
        }

        public static double AngleLines(PointF[] points1, PointF[] points2)
        {
            if (points1.Length != points2.Length || points1.Length < 2)
                throw new HalconException("输入参数的长度必须为相等且大于2");
            var line1 = SplitLine(points1);
            var line2 = SplitLine(points2);
            HOperatorSet.AngleLl(line1[0], line1[1], line1[2], line1[3], line2[0], line2[1], line2[2], line2[3], out var angle);
            return angle.D;
        }
        private static double[] SplitLine(PointF[] points)
        {
            double row1 = points[0].X;
            double column1 = points[0].Y;
            double row2 = points[1].X;
            double column2 = points[1].Y;
            double[] result = { row1, column1, row2, column2 };
            return result;
        }
        public static PointF[] AffineTransPoints(PointF[] points, double[] transMatrix)
        {
            PointF[] transPoints = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                transPoints[i] = AffineTransPoint(points[i], transMatrix);
            }
            return transPoints;
        }
        public static PointF AffineTransPoint(PointF point, double[] transMatrix)
        {
            HOperatorSet.AffineTransPoint2d(transMatrix, (double)point.X, (double)point.Y, out var transPointX, out var transPointY);
            return new PointF((float)transPointX.D, (float)transPointY.D);
        }
    }
}

