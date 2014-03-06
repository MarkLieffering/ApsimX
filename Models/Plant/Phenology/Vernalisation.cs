using Models.Core;
using Models.PMF.Functions;
using System;

namespace Models.PMF.Phen
{

    [Serializable]
    public class Vernalisation
    {
        [Link]
        Phenology Phenology = null;

        [Link]
        AirTemperatureFunction VDModel = null;

        public string StartStage = "";
        public string EndStage = "";
        
        private double CumulativeVD = 0;

        /// <summary>
        /// Trap the NewMet event.
        /// </summary>
        [EventSubscribe("NewMet")]
        private void OnNewMet(Models.WeatherFile.NewMetType NewMet)
        {
            if (Phenology.Between(StartStage, EndStage))
                DoVernalisation(NewMet.maxt, NewMet.mint);
        }

        /// <summary>
        /// Initialise everything
        /// </summary>
        public void OnCommencing()
        {
            CumulativeVD = 0;
        }

        /// <summary>
        /// Do our vernalisation
        /// </summary>
        public void DoVernalisation(double Maxt, double Mint)
        {
            CumulativeVD += VDModel.Value;
        }


    }
}