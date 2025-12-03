using System;
using System.Collections.Generic;

namespace StationControl.Models.Util
{
    public class StatutBrandStats
    {
        public string BrandName { get; set; }
        public int TotalStations { get; set; }

        public int Fonctionnelle { get; set; }
        public int Partielle { get; set; }
        public int NonFonctionnelle { get; set; }

        public double TauxFonctionnelle { get; set; }
        public double TauxPartielle { get; set; }
        public double TauxNonFonctionnelle { get; set; }
    }

}
