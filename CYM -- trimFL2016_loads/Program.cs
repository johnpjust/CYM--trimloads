using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Device.Location;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CYM____trimFL2016_loads
{
    static class Program
    {

        public class Win32APIs
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr LoadLibrary(string lpszLib);
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int GetTickCount();

        }

        public static class GSL
        {
            //load the library
            public static IntPtr libptr = Win32APIs.LoadLibrary("scaleFilter.dll");

            //load model and relevant functions
            [DllImport("scaleFilter.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void MBSD_SYM_initialize();
            [DllImport("scaleFilter.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void MBSD_SYM_custom(double[] arg_scaleData, double[] arg_scaleDataFilt);


            ////load the library
            //public static IntPtr libptr2 = Win32APIs.LoadLibrary("scale_filt2.dll");

            ////load model and relevant functions
            //[DllImport("scale_filt2.dll", CallingConvention = CallingConvention.Cdecl)]
            //public static extern void MBSD_SYM_initialize(int s);
            //[DllImport("scale_filt2.dll", CallingConvention = CallingConvention.Cdecl)]
            //public static extern void MBSD_SYM_custom(double[] arg_scaleData, double[] arg_scaleDataFilt);
            //[DllImport("scale_filt2.dll", CallingConvention = CallingConvention.Cdecl)]
            //public static extern void MBSD_SYM_terminate();

        }

        class harvData
        {
            public double scale;
            public double scaleFilt;
            public DateTime unixtime;
            public double lat;
            public double lon;
            public double gpsSPD;
            public double fpgaTime;
            public double elevSpd;
            public double instVol;
            public double trashsig;
            public double lgain;
            public double rgain;
        }

        static void Main(string[] args)
        {

            ITSsqlInternalCANDataContext sugarDB = new ITSsqlInternalCANDataContext();
            string direct = @"C:\Users\Just\Downloads\New folder"; //directory with harvester data
            Directory.CreateDirectory(direct);


            var filesTractor = (from ent in sugarDB.TractorLocations
                                where ent.GPS_DateTime > new DateTime(2016, 2, 9) & ent.GPS_DateTime < new DateTime(2016, 3, 13)
                                select ent).ToList();

            //////////////////////////////   Process through stereo logs /////////////////////////////////////

            string[] files = Directory.GetFiles(@"Z:\Sugarcane\2016-Prototype\TranslatedLogs\2016-02-11", "*log.v118c.txt", SearchOption.TopDirectoryOnly);

            //string[] files = { @"C:\Users\Just\Downloads\2016-02-05\Logger00-d0-c9-ea-a1-c7_2016-02-05_055938_00012.log.txt" };

            //LOG_TIMESTAMP	GPS_TIMESTAMP	LATITUDE	LONGITUDE	ALTITUDE	GROUND_SPEED	ELEVATOR_SPEED	INTEGRATED_VOLUME	SCALE_1	SCALE_2	INSTANTANEOUS_VOLUME	TRASH_RATIO	PRIMARY_FAN_EXTRACTOR_SPEED	FRAME_ID	SCALED_INTEGRATED_VOLUME
            //LEFT_CAMERA_GAIN	RIGHT_CAMERA_GAIN	VALID_DISPARITY_RATIO	NUM_CELLS_WITH_VOLUME	STUCK_MATERIAL_VALUE	DIRTY_LENS_VALUE	STUCK_MATERIAL_FILT_VALUE	DIRTY_LENS_FILT_VALUE	HARVEST_MODE	CAMERA_ANGLE_ACCELEROMETERS	
            //STATUS_BIAS_CALC	BIAS_VOLUME	INSTANTANEOUS_VOLUME_20_PERCENTILE	INSTANTANEOUS_VOLUME_30_PERCENTILE	INSTANTANEOUS_VOLUME_40_PERCENTILE	INSTANTANEOUS_VOLUME_50_PERCENTILE	INSTANTANEOUS_VOLUME_60_PERCENTILE	
            //INSTANTANEOUS_VOLUME_70_PERCENTILE	INSTANTANEOUS_VOLUME_80_PERCENTILE	INSTANTANEOUS_VOLUME_90_PERCENTILE

            foreach (string f in files)
            {
                string IDmatch = "[a-f0-9][a-f0-9]_2016";
                Regex rmat = new Regex(IDmatch, RegexOptions.IgnoreCase);
                string mmat = rmat.Match(Path.GetFileNameWithoutExtension(f)).ToString().Substring(0, 2);
                int delay;
                switch (mmat)
                {
                    case "cd":
                        delay = 0;// 86400;// -196; //196 sec delay 
                        break;
                    case "a1":
                        delay = 0;// 86400;// -25119;// -25343;// 24988; //24950
                        break;
                    case "c7":
                        delay = 0;// 86400;// 24748;
                        break;
                    case "c9": //c9 is the old 7b machine
                        delay = 0;// 86400;// 24988; //almost 7 hours ahead 24988
                        break;
                    case "7b":
                        delay = 0;// 86400;// -25243;
                        break;
                    default:
                        delay = 0;
                        break;
                }

                List<harvData> hdat = new List<harvData>();
                using (StreamReader sr = new StreamReader(f))
                {
                    string line;
                    for (int i = 0; i <= 10; i++)
                    {
                        sr.ReadLine();
                    }
                    string[] linearr;

                    while ((line = sr.ReadLine()) != null)
                    {
                        linearr = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        harvData hd = new harvData();
                        hd.unixtime = UnixTimeStampToDateTime(Math.Round(Convert.ToDouble(linearr[1])), delay);
                        if (hd.unixtime.Year == 2016)
                        {
                            hd.lon = Convert.ToDouble(linearr[3]);
                            if (hd.lon != 0 & Math.Abs(hd.lon) < 90)
                            {
                                hd.scale = Convert.ToDouble(linearr[8]);
                                hd.lat = Convert.ToDouble(linearr[2]);
                                hd.gpsSPD = Convert.ToDouble(linearr[5]);
                                hd.fpgaTime = Convert.ToDouble(linearr[0]);
                                hd.elevSpd = Convert.ToDouble(linearr[6]);
                                hd.instVol = Convert.ToDouble(linearr[10]);
                                hd.trashsig = Convert.ToDouble(linearr[11]);
                                hd.lgain = Convert.ToDouble(linearr[15]);
                                hd.rgain = Convert.ToDouble(linearr[16]);
                                hdat.Add(hd);
                            }

                        }
                    }
                }
                double[] managedArray = new double[1000];
                double[] scalein = new double[1000];
                int length = Convert.ToUInt16(Math.Floor(hdat.Count() / 1000.0));

                if (hdat.Count() > 1000 && hdat.Any(P => P.scale > 3000 & P.scale < 30000))
                {
                    hdat.RemoveRange(length * 1000, hdat.Count() - length * 1000);

                    for (int nout = 0; nout < length; nout++)
                    {
                        for (int n = 0; n < 1000; n++)
                        {
                            scalein[n] = hdat[nout * 1000 + n].scale;
                        }
                        GSL.MBSD_SYM_custom(scalein, managedArray);
                        for (int n = 0; n < 1000; n++)
                        {
                            hdat[nout * 1000 + n].scaleFilt = managedArray[n];
                        }
                    }
                    //int length = scale.Count();
                    //GSL.MBSD_SYM_initialize(length);
                    //double[] scaleFilt = new double[length];
                    //GSL.MBSD_SYM_custom(scale.ToArray(), scaleFilt);
                    //GSL.MBSD_SYM_terminate();

                    List<harvData> hdat_Loads = new List<harvData>();
                    hdat_Loads = hdat.GroupBy(t => new { time = t.unixtime })
                   .Select(g => new harvData
                   {
                       scale = g.Average(p => p.scale),
                       scaleFilt = g.Average(p => p.scaleFilt),
                       lat = g.Average(p => p.lat),
                       lon = g.Average(p => p.lon),
                       gpsSPD = g.Average(p => p.gpsSPD),
                       fpgaTime = g.Average(p => p.fpgaTime),
                       elevSpd = g.Average(p => p.elevSpd),
                       instVol = g.Average(p => p.instVol),
                       trashsig = g.Average(p => p.trashsig),
                       lgain = g.Average(p => p.lgain),
                       rgain = g.Average(p => p.rgain),
                       unixtime = g.Key.time
                   }).ToList();

                    //hdat_Loads = hdat_Loads.Where(scalefilt => scalefilt.scaleFilt > 0).ToList();
                    if (hdat_Loads.Count > 0)
                    {
                            //var query = (from hdat_el in hdat_Loads
                            //             join trac_el in tracList[datmatch_index].dat on hdat_el.unixtime equals trac_el.dateTime_trac
                            //             select new { hdat_el, trac_el }).Distinct().ToList();



                            var query = (from hdat_el in hdat_Loads
                                         join trac_el in filesTractor on hdat_el.unixtime equals trac_el.GPS_DateTime into gj
                                         from subpet in gj.DefaultIfEmpty()
                                         select new { hdat_el, trac_el = (subpet == null ? new TractorLocation() : subpet) }).ToList();

                        //List<double> distanceTo = new List<double>();
                        //foreach (var q in query)
                        //{
                        //    GeoCoordinate trac_loc = new GeoCoordinate(q.trac_el.lat, q.trac_el.lon);
                        //    GeoCoordinate harv_loc = new GeoCoordinate(q.hdat_el.lat, q.hdat_el.lon);
                        //    distanceTo.Add(trac_loc.GetDistanceTo(harv_loc));
                        //}

                        using (StreamWriter sw = new StreamWriter(Path.Combine(direct, Path.GetFileNameWithoutExtension(f) + ".csv")))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Clear();
                            sb.Append("fpgaTime").Append(",").Append("RawScaleHarv").Append(",").Append("FiltScaleHarv").Append(",").Append("UTCharv").Append(",").Append("latHarv").Append(",").Append("lonHarv");
                            sb.Append(",").Append("elevSpd").Append(",").Append("instVol").Append(",").Append("linSum").Append(",").Append("sqrtSum").Append(",").Append("trashsig").Append(",").Append("lgain").Append(",").Append("rgain");
                            sb.Append(",").Append("GPSspdHarv").Append(",").Append("UTCtrac").Append(",").Append("latTrac").Append(",").Append("lonTrac").Append(",").Append("DistBetweenH&T");
                            sb.Append(",").Append("GPSspdTrac").Append(",").Append("machID").Append(",").Append("Filename");
                            sw.WriteLine(sb);

                            double linSum = 0;
                            double sqrtSum = 0;
                            double prevTime = 0;
                            double deltaTime = 0;

                            foreach (var q in query)
                            {
                                GeoCoordinate trac_loc = new GeoCoordinate(90, 90);
                                GeoCoordinate harv_loc = new GeoCoordinate(0, 0);
                                if (q.trac_el.CAN_Latitude != null && (Math.Abs((double)q.trac_el.CAN_Latitude) < 90 & Math.Abs((double)q.trac_el.CAN_Longitude) < 90))
                                {
                                    trac_loc = new GeoCoordinate((double)q.trac_el.CAN_Latitude, (double)q.trac_el.CAN_Longitude);

                                }

                                if (Math.Abs(q.hdat_el.lat) < 90 & Math.Abs(q.hdat_el.lon) < 90)
                                {

                                    harv_loc = new GeoCoordinate(q.hdat_el.lat, q.hdat_el.lon);
                                }

                                deltaTime = q.hdat_el.fpgaTime - prevTime;
                                prevTime = q.hdat_el.fpgaTime;
                                if (deltaTime > 5) deltaTime = 1;
                                linSum += q.hdat_el.elevSpd * deltaTime * (Math.Max(q.hdat_el.instVol - 1, 0));
                                sqrtSum += q.hdat_el.elevSpd * deltaTime * Math.Sqrt(Math.Max(q.hdat_el.instVol - 1, 0));

                                sb.Clear();
                                sb.Append(q.hdat_el.fpgaTime).Append(",").Append(q.hdat_el.scale).Append(",").Append(q.hdat_el.scaleFilt).Append(",").Append(q.hdat_el.unixtime).Append(",").Append(q.hdat_el.lat).Append(",").Append(q.hdat_el.lon);
                                sb.Append(",").Append(q.hdat_el.elevSpd).Append(",").Append(q.hdat_el.instVol).Append(",").Append(linSum).Append(",").Append(sqrtSum).Append(",").Append(q.hdat_el.trashsig).Append(",").Append(q.hdat_el.lgain);
                                sb.Append(",").Append(q.hdat_el.rgain).Append(",").Append(q.hdat_el.gpsSPD);
                                sb.Append(",").Append(q.trac_el.GPS_DateTime).Append(",").Append(q.trac_el.CAN_Latitude).Append(",").Append(q.trac_el.CAN_Longitude).Append(",").Append(trac_loc.GetDistanceTo(harv_loc));
                                sb.Append(",").Append(q.trac_el.CAN_NavigationBasedVehicleSpeed_kph).Append(",").Append(mmat);
                                sw.WriteLine(sb);
                            }
                        }
                        
                    }
                }
            }

            /////////////////// combine all output files just created ///////////////////////
            //00-d0-c9-ea-a1-c7 --> red/4648
            //00-d0-c9-ea-8f-cd --> Yellow/4639
            //00-d0-c9-ea-a1-a1 --> Green/4644
            //00-d0-c9-ea-a1-c9 --> blue
            combineFiles(direct);

        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp, int delay)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).AddSeconds(delay);
            return dtDateTime;
        }

        public static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (TimeZoneInfo.ConvertTimeToUtc(dateTime) -
                   new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
        }
        public static void combineFiles(string foldername)
        {

            string dirName = new DirectoryInfo(foldername).Name;
            string newfilename = Path.Combine(foldername, dirName + "_Combined.csv");
            char sep = ',';
            if (File.Exists(newfilename)) File.Delete(newfilename);
            string[] filesINfolder = Directory.GetFiles(foldername, "*.csv", SearchOption.AllDirectories);


            bool writeheader = true;
            List<String> newlinelist = new List<String>();
            List<string> lineList = new List<string>();
            foreach (string filename in filesINfolder)
            {
                FileInfo f;
                if ((f = new FileInfo(filename)).Length > 3000)
                {
                    if (writeheader)
                    {
                        lineList.AddRange(File.ReadAllLines(filename));
                        foreach (string li in lineList)
                        {
                            if (li.Split(new Char[] { sep }, StringSplitOptions.RemoveEmptyEntries).Length > 6) {
                                newlinelist.Add(li + sep + Path.GetFileNameWithoutExtension(filename));
                            }

                        }
                        File.WriteAllLines(newfilename, newlinelist);
                        writeheader = false;
                    }
                    
                    else
                    {
                        lineList.Clear();
                        newlinelist.Clear();
                        lineList.AddRange(File.ReadAllLines(filename));
                        lineList.RemoveRange(0, 2);
                        foreach(string li in lineList)
                        {
                            if(li.Split(new Char[] {sep}, StringSplitOptions.RemoveEmptyEntries).Length > 6)
                            {
                                newlinelist.Add(li + sep + Path.GetFileNameWithoutExtension(filename));
                            }

                        }
                        File.AppendAllLines(newfilename, newlinelist);


                    }

                }

            }
        }
    }   
}
