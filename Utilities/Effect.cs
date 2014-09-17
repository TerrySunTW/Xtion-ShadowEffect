using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Reflection;
using System.Drawing.Imaging;
namespace GdipEffect
{
    // ***************** GDI+ Effect函數的示例代碼 *********************
    // 作者     ： laviewpbt 
    // 作者簡介 ： 對影像處理（非識別）有著較深程度的理解
    // 使用語言 ： VB6.0/C#/VB.NET
    // 聯繫方式 ： QQ-33184777  E-Mail:laviewpbt@sina.com
    // 開發時間 ： 2012.12.10-2012.12.12
    // 致謝     ： Aaron Lee Murgatroyd
    // 版權聲明 ： 複製或轉載請保留以上個人資訊
    // *****************************************************************

    public static class Effect
    {
        private static Guid BlurEffectGuid = new Guid("{633C80A4-1843-482B-9EF2-BE2834C5FDD4}");
        private static Guid UsmSharpenEffectGuid = new Guid("{63CBF3EE-C526-402C-8F71-62C540BF5142}");

        [StructLayout(LayoutKind.Sequential)]
        private struct BlurParameters
        {
            internal float Radius;
            internal bool ExpandEdges;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SharpenParams
        {
            internal float Radius;
            internal float Amount;
        }

        internal enum PaletteType               // GDI+1.1還可以針對一副圖像獲取某種特殊的調色
        {
            PaletteTypeCustom = 0,
            PaletteTypeOptimal = 1,
            PaletteTypeFixedBW = 2,
            PaletteTypeFixedHalftone8 = 3,
            PaletteTypeFixedHalftone27 = 4,
            PaletteTypeFixedHalftone64 = 5,
            PaletteTypeFixedHalftone125 = 6,
            PaletteTypeFixedHalftone216 = 7,
            PaletteTypeFixedHalftone252 = 8,
            PaletteTypeFixedHalftone256 = 9
        };

        internal enum DitherType                    // 這個主要用於將真彩色圖像轉換為索引圖像，並儘量減低顏色損失
        {
            DitherTypeNone = 0,
            DitherTypeSolid = 1,
            DitherTypeOrdered4x4 = 2,
            DitherTypeOrdered8x8 = 3,
            DitherTypeOrdered16x16 = 4,
            DitherTypeOrdered91x91 = 5,
            DitherTypeSpiral4x4 = 6,
            DitherTypeSpiral8x8 = 7,
            DitherTypeDualSpiral4x4 = 8,
            DitherTypeDualSpiral8x8 = 9,
            DitherTypeErrorDiffusion = 10
        }


        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipCreateEffect(Guid guid, out IntPtr effect);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipDeleteEffect(IntPtr effect);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipGetEffectParameterSize(IntPtr effect, out uint size);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipSetEffectParameters(IntPtr effect, IntPtr parameters, uint size);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipGetEffectParameters(IntPtr effect, ref uint size, IntPtr parameters);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipBitmapApplyEffect(IntPtr bitmap, IntPtr effect, ref Rectangle rectOfInterest, bool useAuxData, IntPtr auxData, int auxDataSize);

        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipBitmapCreateApplyEffect(ref IntPtr SrcBitmap, int numInputs, IntPtr effect, ref Rectangle rectOfInterest, ref Rectangle outputRect, out IntPtr outputBitmap, bool useAuxData, IntPtr auxData, int auxDataSize);


        // 這個函數我在C#下已經調用成功
        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipInitializePalette(IntPtr palette, int palettetype, int optimalColors, int useTransparentColor, int bitmap);

        // 該函數一致不成功，不過我在VB6下調用很簡單，也很成功，主要是結構體的問題。
        [DllImport("gdiplus.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int GdipBitmapConvertFormat(IntPtr bitmap, int pixelFormat, int dithertype, int palettetype, IntPtr palette, float alphaThresholdPercent);

        /// <summary>
        /// 獲取對象的私有欄位的值，感謝Aaron Lee Murgatroyd
        /// </summary>
        /// <typeparam name="TResult">欄位的類型</typeparam>
        /// <param name="obj">要從其中獲取欄位值的對象</param>
        /// <param name="fieldName">欄位的名稱.</param>
        /// <returns>欄位的值</returns>
        /// <exception cref="System.InvalidOperationException">無法找到該欄位.</exception>
        /// 
        internal static TResult GetPrivateField<TResult>(this object obj, string fieldName)
        {
            if (obj == null) return default(TResult);
            Type ltType = obj.GetType();
            FieldInfo lfiFieldInfo = ltType.GetField(fieldName, System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (lfiFieldInfo != null)
                return (TResult)lfiFieldInfo.GetValue(obj);
            else
                throw new InvalidOperationException(string.Format("Instance field '{0}' could not be located in object of type '{1}'.", fieldName, obj.GetType().FullName));
        }

        public static IntPtr NativeHandle(this Bitmap Bmp)
        {
            return Bmp.GetPrivateField<IntPtr>("nativeImage");
            /*  用Reflector反編譯System.Drawing.Dll可以看到Image類有如下的私有欄位
                internal IntPtr nativeImage;
                private byte[] rawData;
                private object userData;
                然後還有一個 SetNativeImage函數
                internal void SetNativeImage(IntPtr handle)
                {
                    if (handle == IntPtr.Zero)
                    {
                        throw new ArgumentException(SR.GetString("NativeHandle0"), "handle");
                    }
                    this.nativeImage = handle;
                }
                這裡在看看FromFile等等函數，其實也就是調用一些例如GdipLoadImageFromFile之類的GDIP函數，並把返回的GDIP圖像控制碼
                通過調用SetNativeImage賦值給變數nativeImage，因此如果我們能獲得該值，就可以調用VS2010暫時還沒有封裝的GDIP函數
                進行相關處理了，並且由於.NET肯定已經初始化過了GDI+，我們也就無需在調用GdipStartup初始化他了。
             */
        }

        public static void Invert(this Bitmap bimage)
        {
            // Step 1: 先鎖住存放圖片的記憶體
            BitmapData bmData = bimage.LockBits(new Rectangle(0, 0, bimage.Width, bimage.Height),
            ImageLockMode.ReadWrite,
           PixelFormat.Format24bppRgb);
            int stride = bmData.Stride;
            // Step 2: 取得像點資料的起始位址 
            System.IntPtr Scan0 = bmData.Scan0;
            // 計算每行的像點所佔據的byte 總數
            int ByteNumber_Width = bimage.Width * 3;
            // 計算每一行後面幾個 Padding bytes
            int ByteOfSkip = stride - ByteNumber_Width;
            // Step 3: 直接利用指標, 更改圖檔的內容
            int Height = bimage.Height;
            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < ByteNumber_Width; x++)
                    {
                        p[0] = (byte)(255 - p[0]); // 彩色資料反轉
                        ++p;
                    }
                    p += ByteOfSkip; // 跳過剩下的 Padding bytes
                }
            }
            bimage.UnlockBits(bmData);
        }

        public static void Lighter(this Bitmap bimage, int Times)
        {
            // Step 1: 先鎖住存放圖片的記憶體
            BitmapData bmData = bimage.LockBits(new Rectangle(0, 0, bimage.Width, bimage.Height),
            ImageLockMode.ReadWrite,
           PixelFormat.Format24bppRgb);
            int stride = bmData.Stride;
            // Step 2: 取得像點資料的起始位址 
            System.IntPtr Scan0 = bmData.Scan0;
            // 計算每行的像點所佔據的byte 總數
            int ByteNumber_Width = bimage.Width * 3;
            // 計算每一行後面幾個 Padding bytes
            int ByteOfSkip = stride - ByteNumber_Width;
            // Step 3: 直接利用指標, 更改圖檔的內容
            int Height = bimage.Height;
            unsafe
            {
                byte* p = (byte*)(void*)Scan0;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < ByteNumber_Width; x++)
                    {
                        if (p[0] == 0)
                        {
                            p[0] = (byte)(p[0] + 25 * Times);
                        }

                        ++p;
                    }
                    p += ByteOfSkip; // 跳過剩下的 Padding bytes
                }
            }
            bimage.UnlockBits(bmData);
        }

        /// <summary>
        /// 對圖像進行高斯模糊,參考：http://msdn.microsoft.com/en-us/library/ms534057(v=vs.85).aspx
        /// </summary>
        /// <param name="Rect">需要模糊的區域，會對該值進行邊界的修正並返回.</param>
        /// <param name="Radius">指定高斯卷積核的半徑，有效範圍[0，255],半徑越大，圖像變得越模糊.</param>
        /// <param name="ExpandEdge">指定是否對邊界進行擴展，設置為True，在邊緣處可獲得較為柔和的效果. </param>

        public static void GaussianBlur(this Bitmap Bmp, ref Rectangle Rect, float Radius = 10, bool ExpandEdge = false)
        {
            int Result;
            IntPtr BlurEffect;
            BlurParameters BlurPara;
            if ((Radius < 0) || (Radius > 255))
            {
                throw new ArgumentOutOfRangeException("半徑必須在[0,255]範圍內");
            }
            BlurPara.Radius = Radius;
            BlurPara.ExpandEdges = ExpandEdge;
            Result = GdipCreateEffect(BlurEffectGuid, out BlurEffect);
            if (Result == 0)
            {
                IntPtr Handle = Marshal.AllocHGlobal(Marshal.SizeOf(BlurPara));
                Marshal.StructureToPtr(BlurPara, Handle, true);
                GdipSetEffectParameters(BlurEffect, Handle, (uint)Marshal.SizeOf(BlurPara));
                GdipBitmapApplyEffect(Bmp.NativeHandle(), BlurEffect, ref Rect, false, IntPtr.Zero, 0);
                // 使用GdipBitmapCreateApplyEffect函數可以不改變原始的圖像，而把模糊的結果寫入到一個新的圖像中
                GdipDeleteEffect(BlurEffect);
                Marshal.FreeHGlobal(Handle);
            }
            else
            {
                throw new ExternalException("不支持的GDI+版本，必須為GDI+1.1及以上版本，且作業系統要求為Win Vista及之後版本.");
            }
        }


        /// <summary>
        /// 對圖像進行銳化,參考：http://msdn.microsoft.com/en-us/library/ms534073(v=vs.85).aspx
        /// </summary>
        /// <param name="Rect">需要銳化的區域，會對該值進行邊界的修正並返回.</param>
        /// <param name="Radius">指定高斯卷積核的半徑，有效範圍[0，255],因為這個銳化演算法是以高斯模糊為基礎的，所以他的速度肯定比高斯模糊媽媽</param>
        /// <param name="ExpandEdge">指定銳化的程度，0表示不銳化。有效範圍[0,255]. </param>
        /// 
        public static void UsmSharpen(this Bitmap Bmp, ref Rectangle Rect, float Radius = 10, float Amount = 50f)
        {
            int Result;
            IntPtr UnSharpMaskEffect;
            SharpenParams sharpenParams;
            if ((Radius < 0) || (Radius > 255))
            {
                throw new ArgumentOutOfRangeException("參數Radius必須在[0,255]範圍內");
            }
            if ((Amount < 0) || (Amount > 100))
            {
                throw new ArgumentOutOfRangeException("參數Amount必須在[0,255]範圍內");
            }
            sharpenParams.Radius = Radius;
            sharpenParams.Amount = Amount;
            Result = GdipCreateEffect(UsmSharpenEffectGuid, out UnSharpMaskEffect);
            if (Result == 0)
            {
                IntPtr Handle = Marshal.AllocHGlobal(Marshal.SizeOf(sharpenParams));
                Marshal.StructureToPtr(sharpenParams, Handle, true);
                GdipSetEffectParameters(UnSharpMaskEffect, Handle, (uint)Marshal.SizeOf(sharpenParams));
                GdipBitmapApplyEffect(Bmp.NativeHandle(), UnSharpMaskEffect, ref Rect, false, IntPtr.Zero, 0);
                GdipDeleteEffect(UnSharpMaskEffect);
                Marshal.FreeHGlobal(Handle);
            }
            else
            {
                throw new ExternalException("不支持的GDI+版本，必須為GDI+1.1及以上版本，且作業系統要求為Win Vista及之後版本.");
            }
        }
    }
}

