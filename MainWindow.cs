/****************************************************************************
*                                                                           *
*  OpenNI 1.x Alpha                                                         *
*  Copyright (C) 2011 PrimeSense Ltd.                                       *
*                                                                           *
*  This file is part of OpenNI.                                             *
*                                                                           *
*  OpenNI is free software: you can redistribute it and/or modify           *
*  it under the terms of the GNU Lesser General Public License as published *
*  by the Free Software Foundation, either version 3 of the License, or     *
*  (at your option) any later version.                                      *
*                                                                           *
*  OpenNI is distributed in the hope that it will be useful,                *
*  but WITHOUT ANY WARRANTY; without even the implied warranty of           *
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the             *
*  GNU Lesser General Public License for more details.                      *
*                                                                           *
*  You should have received a copy of the GNU Lesser General Public License *
*  along with OpenNI. If not, see <http://www.gnu.org/licenses/>.           *
*                                                                           *
****************************************************************************/
﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenNI;
using System.Threading;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Diagnostics;
using GdipEffect;
using System.Runtime.InteropServices;
using UserTracker.net.Utilities;
using ShadowEffect;
namespace ShadowEffect
{
	public partial class MainWindow : Form
	{
        FormState formState = new FormState();
		public MainWindow()
		{
			InitializeComponent();
            //full screen
            this.Left = this.Top = 0;
            this.Width = Screen.PrimaryScreen.WorkingArea.Width;
            this.Height = Screen.PrimaryScreen.WorkingArea.Height;
            formState.Maximize(this);
            this.TopMost = true;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.Location = Screen.PrimaryScreen.WorkingArea.Location;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            Taskbar.Hide();
            
			this.context = Context.CreateFromXmlFile(SAMPLE_XML_FILE, out scriptNode);
			this.depth = context.FindExistingNode(NodeType.Depth) as DepthGenerator;
			if (this.depth == null)
			{
				throw new Exception("Viewer must have a depth node!");
			}

            this.userGenerator = new UserGenerator(this.context);
			this.skeletonCapbility = this.userGenerator.SkeletonCapability;
			this.poseDetectionCapability = this.userGenerator.PoseDetectionCapability;
            this.calibPose = this.skeletonCapbility.CalibrationPose;

            this.userGenerator.NewUser += userGenerator_NewUser;
            this.userGenerator.LostUser += userGenerator_LostUser;
            this.poseDetectionCapability.PoseDetected += poseDetectionCapability_PoseDetected;
            this.skeletonCapbility.CalibrationComplete += skeletonCapbility_CalibrationComplete;

            this.skeletonCapbility.SetSkeletonProfile(SkeletonProfile.All);
            this.joints = new Dictionary<int,Dictionary<SkeletonJoint,SkeletonJointPosition>>();
            this.userGenerator.StartGenerating();


			this.histogram = new int[this.depth.DeviceMaxDepth];

			MapOutputMode mapMode = this.depth.MapOutputMode;

			this.bitmap = new Bitmap((int)mapMode.XRes, (int)mapMode.YRes/*, System.Drawing.Imaging.PixelFormat.Format24bppRgb*/);
			this.shouldRun = true;
			this.readerThread = new Thread(ReaderThread);
			this.readerThread.Start();
		}

        void skeletonCapbility_CalibrationComplete(object sender, CalibrationProgressEventArgs e)
        {
            if (e.Status == CalibrationStatus.OK)
            {
                this.skeletonCapbility.StartTracking(e.ID);
                this.joints.Add(e.ID, new Dictionary<SkeletonJoint, SkeletonJointPosition>());
            }
            else if (e.Status != CalibrationStatus.ManualAbort)
            {
                if (this.skeletonCapbility.DoesNeedPoseForCalibration)
                {
                    this.poseDetectionCapability.StartPoseDetection(calibPose, e.ID);
                }
                else
                {
                    this.skeletonCapbility.RequestCalibration(e.ID, true);
                }
            }
        }

        void poseDetectionCapability_PoseDetected(object sender, PoseDetectedEventArgs e)
        {
            this.poseDetectionCapability.StopPoseDetection(e.ID);
            this.skeletonCapbility.RequestCalibration(e.ID, true);
        }

        void userGenerator_NewUser(object sender, NewUserEventArgs e)
        {
            if (this.skeletonCapbility.DoesNeedPoseForCalibration)
            {
                this.poseDetectionCapability.StartPoseDetection(this.calibPose, e.ID);
            }
            else
            {
                this.skeletonCapbility.RequestCalibration(e.ID, true);
            }
        }

		void userGenerator_LostUser(object sender, UserLostEventArgs e)
        {
            this.joints.Remove(e.ID);
        }
        Queue ImgQueue = new Queue();
        int ImgQueueLitmitation = App.Default.ShadowQuantity;//殘影的數量
        int ImgCounter = 0;
        int saveLimit = App.Default.ShadowSpeed;//每幾個影格存一個 //被lock住，還沒有處理完之前，不會有新影像進來
        
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			lock (this)
			{
                if (this.bitmap !=null)
                { 
                    //建一個queue，只畫前幾個畫面
                    ImgCounter++;
                    if (App.Default.IsBlackShadow)
                    {
                        this.bitmap.Invert();
                    }
                
                

                    if (ImgCounter > saveLimit)
                    {
                        ImgCounter = 0;
                        //去背後存入Queue
                        if (App.Default.IsReverse)
                        {
                            this.bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        }
                    
                        ImgQueue.Enqueue(new Bitmap(this.bitmap));
                    }
                    //LastImg=合成所有Queue的照片，
                    //把所有Queue的圖合成一張，去背、
                    e.Graphics.DrawImage(MergingAllImage(),
                    this.panelView.Location.X,
                    this.panelView.Location.Y,
                    this.panelView.Size.Width,
                    this.panelView.Size.Height);
                }
                
            }
		}
        
        private Bitmap MergingAllImage()
        {
            Bitmap LastBitmap = new Bitmap(this.bitmap.Width, this.bitmap.Height);
            using (Graphics grp = Graphics.FromImage(LastBitmap))
             {
                 var DefaultBackgroundBrushes = Brushes.Black;
                 if (App.Default.IsBlackShadow)
                 {
                     DefaultBackgroundBrushes = Brushes.White;
                 }

                 grp.FillRectangle(
                     DefaultBackgroundBrushes, 0, 0, this.bitmap.Width, this.bitmap.Height);
             }
            Rectangle _Rectangle = new Rectangle(0, 0, LastBitmap.Width, LastBitmap.Height);
            Queue TempQueue = new Queue();
            
            LastBitmap.GaussianBlur(ref _Rectangle, App.Default.ShadowBlurRadio);
            if(ImgQueue.Count > 0)
            {
                var canvas = Graphics.FromImage(LastBitmap);
                while(ImgQueue.Count > 0)
                {
                    
                    Bitmap tmpImg=(Bitmap)ImgQueue.Dequeue();
                    if (App.Default.IsBlackShadow)
                    {
                        tmpImg.MakeTransparent(Color.White);
                    }
                    else
                    {
                        tmpImg.MakeTransparent(Color.Black);
                    }
                    
                    tmpImg.GaussianBlur(ref _Rectangle,App.Default.ShadowBlurRadio);
                    
                    if (ImgQueue.Count < ImgQueueLitmitation)
                    {
                        TempQueue.Enqueue(tmpImg);
                    }

                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix33 = App.Default.ShadowAlpha;
                    ImageAttributes _ImageAttributes = new ImageAttributes();
                    _ImageAttributes.SetColorMatrix(cm);


                    canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    canvas.DrawImage(tmpImg, new Rectangle(0, 0, this.panelView.Size.Width, this.panelView.Size.Height),
                        0, 0, this.panelView.Size.Width, this.panelView.Size.Height,GraphicsUnit.Pixel,_ImageAttributes);
                    canvas.Save();
                       
                }
                ImgQueue = TempQueue;
                ImgQueue.TrimToSize();
            }
            return LastBitmap;
        }
		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			//Don't allow the background to paint
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.shouldRun = false;
			this.readerThread.Join();
			base.OnClosing(e);
		}
		private unsafe void CalcHist(DepthMetaData depthMD)
		{
			// reset
			for (int i = 0; i < this.histogram.Length; ++i)
				this.histogram[i] = 0;

			ushort* pDepth = (ushort*)depthMD.DepthMapPtr.ToPointer();

			int points = 0;
			for (int y = 0; y < depthMD.YRes; ++y)
			{
				for (int x = 0; x < depthMD.XRes; ++x, ++pDepth)
				{
					ushort depthVal = *pDepth;
					if (depthVal != 0)
					{
						this.histogram[depthVal]++;
						points++;
					}
				}
			}

			for (int i = 1; i < this.histogram.Length; i++)
			{
				this.histogram[i] += this.histogram[i-1];
			}

			if (points > 0)
			{
				for (int i = 1; i < this.histogram.Length; i++)
				{
					this.histogram[i] = (int)(256 * (1.0f - (this.histogram[i] / (float)points)));
				}
			}
		}

        private Color[] colors = { Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black, Color.Black };
        private Color[] anticolors = { Color.Green, Color.Orange, Color.Red, Color.Purple, Color.Blue, Color.Yellow, Color.White};
        private int ncolors = 6;

        private void GetJoint(int user, SkeletonJoint joint)
        {
            SkeletonJointPosition pos = this.skeletonCapbility.GetSkeletonJointPosition(user, joint);
			if (pos.Position.Z == 0)
			{
				pos.Confidence = 0;
			}
			else
			{
				pos.Position = this.depth.ConvertRealWorldToProjective(pos.Position);
			}
			this.joints[user][joint] = pos;
        }

		private unsafe void ReaderThread()
		{
			DepthMetaData depthMD = new DepthMetaData();

			while (this.shouldRun)
			{
				try
				{
					this.context.WaitOneUpdateAll(this.depth);
				}
				catch (Exception)
				{
				}

				this.depth.GetMetaData(depthMD);

				CalcHist(depthMD);

				lock (this)
				{
					Rectangle rect = new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height);
					BitmapData data = this.bitmap.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                    if (this.shouldDrawPixels)
                    {
                        ushort* pDepth = (ushort*)this.depth.DepthMapPtr.ToPointer();
                        ushort* pLabels = (ushort*)this.userGenerator.GetUserPixels(0).LabelMapPtr.ToPointer();

                        // set pixels
                        for (int y = 0; y < depthMD.YRes; ++y)
                        {
                            byte* pDest = (byte*)data.Scan0.ToPointer() + y * data.Stride;
                            for (int x = 0; x < depthMD.XRes; ++x, ++pDepth, ++pLabels, pDest += 3)
                            {
                                pDest[0] = pDest[1] = pDest[2] = 0;

                                ushort label = *pLabels;
                                if (this.shouldDrawBackground || *pLabels != 0)
                                {
                                    pDest[0] = (byte)(Color.White.B);
                                    pDest[1] = (byte)(Color.White.G);
                                    pDest[2] = (byte)(Color.White.R);
                                }
                            }
                        }
                    }
                    this.bitmap.UnlockBits(data);
                   
                }

				this.Invalidate();
			}
		}
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar.ToString().ToUpper() == Keys.Q.ToString())
            {
                Taskbar.Show();
                Close();
            }
            base.OnKeyPress(e);
        }
		private readonly string SAMPLE_XML_FILE = @"SamplesConfig.xml";

		private Context context;
		private ScriptNode scriptNode;
		private DepthGenerator depth;
        private UserGenerator userGenerator;
        private SkeletonCapability skeletonCapbility;
        private PoseDetectionCapability poseDetectionCapability;
        private string calibPose;
		private Thread readerThread;
		private bool shouldRun;
		private Bitmap bitmap;
		private int[] histogram;

        private Dictionary<int, Dictionary<SkeletonJoint, SkeletonJointPosition>> joints;

        private bool shouldDrawPixels = true;
        private bool shouldDrawBackground = false;
        private bool shouldPrintID = false;
        private bool shouldPrintState = false;
        private bool shouldDrawSkeleton = false;



	}

    
}
