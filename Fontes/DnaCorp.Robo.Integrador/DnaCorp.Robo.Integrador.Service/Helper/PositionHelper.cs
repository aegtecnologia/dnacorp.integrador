using CoordinateSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Service.Helper
{
    public static class PositionHelper
    {
        //referencia: https://coordinatesharp.com/DeveloperGuide
        //referencia: https://www.nuget.org/packages/CoordinateSharp/

        public static void Teste(out string lat, out string lng)
        {
            //var latitude = "022_51_35_4_S";
           // var longitude = "047_10_13_0_W";

            Coordinate c = new Coordinate();
            //c.Latitude = new CoordinatePart(40, 34, 36.552, CoordinatesPosition.N);
            //c.Longitude = new CoordinatePart(70, 45, 24.408, CoordinatesPosition.W);
            c.Latitude = new CoordinatePart(22, 51, 35.400, CoordinatesPosition.S);
            c.Longitude = new CoordinatePart(47, 10, 13.000, CoordinatesPosition.W);

            //c.Latitude.ToDouble();  // Returns 40.57682  (Signed Degree)
            //c.Longitude.ToDouble(); // Returns -70.75678 (Signed Degree)

            lat = c.Latitude.ToDouble().ToString();  // Returns 40.57682  (Signed Degree)
            lng = c.Longitude.ToDouble().ToString(); // Returns -70.75678 (Signed Degree)
        }

        private static CoordinatesPosition TrataPos(string pos)
        {
            if (pos.ToUpper() == "E")
                return CoordinatesPosition.E;
            else if (pos.ToUpper() == "N")
                return CoordinatesPosition.N;
            else if (pos.ToUpper() == "S")
                return CoordinatesPosition.S;
            else
                return CoordinatesPosition.W;
        }
        public static void ConverterPosicaoOmnilink(string x, string y, out string lat, out string lng)
        {
            var arrLat = x.Split('_');
            var arrLng = y.Split('_');

            var latitude = new
            {
                deg = Convert.ToInt32(arrLat[0]),
                min = Convert.ToInt32(arrLat[1]),
                Sec = Convert.ToDouble($"{arrLat[2]},{arrLat[3]}"),
                Pos = TrataPos(arrLat[4])
            };

            var longitude = new
            {
                deg = Convert.ToInt32(arrLng[0]),
                min = Convert.ToInt32(arrLng[1]),
                Sec = Convert.ToDouble($"{arrLng[2]},{arrLng[3]}"),
                Pos = TrataPos(arrLng[4])
            };

            Coordinate c = new Coordinate();

            c.Latitude = new CoordinatePart(latitude.deg, latitude.min, latitude.Sec, latitude.Pos);
            c.Longitude = new CoordinatePart(longitude.deg, longitude.min, longitude.Sec, longitude.Pos);
            
            lat = c.Latitude.ToDouble().ToString();
            lng = c.Longitude.ToDouble().ToString();
        }
    }
}
