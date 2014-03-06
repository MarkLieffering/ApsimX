using System;
using System.Collections.Generic;
using System.Text;

using Models.Core;
using Models.PMF.Functions;
using System.Xml.Serialization;

namespace Models.PMF.Organs
{
    [Serializable]
    public class GenericOrgan : BaseOrgan
    {
        #region Class Dependency Links and Structures
        [Link]
        protected Plant Plant = null;
        #endregion

        #region Class Structures
        private Biomass StartLive = new Biomass();
        #endregion

        #region Class Parameter Function Links
        [Link(IsOptional=true)] Function SenescenceRateFunction = null;
        //[Link] Function DMReallocationFunction = null;
        [Link(IsOptional=true)] Function NReallocationFactor = null;
        [Link(IsOptional=true)] Function NRetranslocationFactor = null;
        [Link] Function NitrogenDemandSwitch = null;
        [Link(IsOptional=true)] Function DMRetranslocationFactor = null;
        [Link] Function StructuralFraction = null; 
        [Link] Function DMDemandFunction = null; 
        [Link(IsOptional=true)] Function InitialWtFunction = null;
        [Link(IsOptional=true)] Function InitialStructuralFraction = null;
        [Link(IsOptional=true)] Function WaterContent = null;  
        [Link] Function MaximumNConc = null;  
        [Link] Function MinimumNConc = null;  
        #endregion

        #region States
        private double SenescenceRate = 0;
        private double _StructuralFraction = 1;
        private double StartNRetranslocationSupply = 0;
        private double StartNReallocationSupply = 0;
        protected double PotentialDMAllocation = 0;
        protected double PotentialStructuralDMAllocation = 0;
        protected double PotentialMetabolicDMAllocation = 0;
        protected double StructuralDMDemand = 0;
        protected double NonStructuralDMDemand = 0;
        protected double InitialWt = 0;
        private double InitStutFraction = 1;

        public override void Clear()
        {
            base.Clear();
            SenescenceRate = 0;
            _StructuralFraction = 1;
            StartNRetranslocationSupply = 0;
            StartNReallocationSupply = 0;
            PotentialDMAllocation = 0;
            PotentialStructuralDMAllocation = 0;
            PotentialMetabolicDMAllocation = 0;
            StructuralDMDemand = 0;
            NonStructuralDMDemand = 0;
            InitialWt = 0;
            InitStutFraction = 1;
            LiveFWt = 0;
        }
        #endregion

        #region Class properties
        
        [XmlIgnore]
        [Units("g/m^2")]
        public double LiveFWt { get; set; }
        #endregion

        #region Organ functions
        public override void DoPotentialDM()
        {
            SenescenceRate = 0;
            if (SenescenceRateFunction != null) //Default of zero means no senescence
                SenescenceRate = SenescenceRateFunction.Value;
            _StructuralFraction = 1;
            if (StructuralFraction != null) //Default of 1 means all biomass is structural
                _StructuralFraction = StructuralFraction.Value;
            InitialWt = 0; //Default of zero means no initial Wt
            if (InitialWtFunction != null)
                InitialWt = InitialWtFunction.Value;
            InitStutFraction = 1.0; //Default of 1 means all initial DM is structural
            if (InitialStructuralFraction != null)
                InitStutFraction = InitialStructuralFraction.Value;

            //Initialise biomass and nitrogen
            if (Live.Wt == 0)
            {
                Live.StructuralWt = InitialWt * InitStutFraction;
                Live.NonStructuralWt = InitialWt * (1 - InitStutFraction);
                Live.StructuralN = Live.StructuralWt * MinimumNConc.Value;
                Live.NonStructuralN = (InitialWt * MaximumNConc.Value) - Live.StructuralN;
            }

            StartLive = Live;
            StartNReallocationSupply = NSupply.Reallocation;
            StartNRetranslocationSupply = NSupply.Retranslocation;

        }
        public override void DoActualGrowth()
        {
            base.DoActualGrowth();

            Live.StructuralWt *= (1.0 - SenescenceRate);
            Live.NonStructuralWt *= (1.0 - SenescenceRate);
            Live.StructuralN *= (1.0 - SenescenceRate);
            Live.NonStructuralN *= (1.0 - SenescenceRate);

            if (WaterContent != null)
                LiveFWt = Live.Wt / (1 - WaterContent.Value);
        }
   
        #endregion

        #region Arbitrator methods
        
        [Units("g/m^2")]
        public override BiomassPoolType DMDemand
        {
            get
            {
             StructuralDMDemand = DMDemandFunction.Value * _StructuralFraction;
             double MaximumDM = (StartLive.StructuralWt + StructuralDMDemand) * 1 / _StructuralFraction;
             MaximumDM = Math.Min(MaximumDM, 10000); // FIXME-EIT Temporary solution: Cealing value of 10000 g/m2 to ensure that infinite MaximumDM is not reached when 0% goes to structural fraction   
             NonStructuralDMDemand = Math.Max(0.0, MaximumDM - StructuralDMDemand - StartLive.StructuralWt - StartLive.NonStructuralWt); 
                return new BiomassPoolType { Structural = StructuralDMDemand, NonStructural = NonStructuralDMDemand };
            }
        }
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in " + Name);
                PotentialMetabolicDMAllocation = value.Metabolic;
                PotentialStructuralDMAllocation = value.Structural;
                PotentialDMAllocation = value.Structural + value.Metabolic;
            }
        }
        public override BiomassSupplyType DMSupply
        {
            get
            {
            double _DMRetranslocationFactor = 0;
            if (DMRetranslocationFactor != null) //Default of 0 means retranslocation is always truned off!!!!
                _DMRetranslocationFactor = DMRetranslocationFactor.Value;
            return new BiomassSupplyType { Fixation = 0, 
                                      Retranslocation = StartLive.NonStructuralWt * _DMRetranslocationFactor,
            Reallocation = 0};
            }
        }
        public override BiomassPoolType NDemand
        {
            get
            {
            double _NitrogenDemandSwitch = 1;
            if (NitrogenDemandSwitch != null) //Default of 1 means demand is always truned on!!!!
                _NitrogenDemandSwitch = NitrogenDemandSwitch.Value;
            double NDeficit = Math.Max(0.0, MaximumNConc.Value * (Live.Wt + PotentialDMAllocation) - Live.N);
            NDeficit *= _NitrogenDemandSwitch;
            double StructuralNDemand = Math.Min(NDeficit,PotentialStructuralDMAllocation * MinimumNConc.Value);
            double NonStructuralNDemand = Math.Max(0,NDeficit - StructuralNDemand); 
                return new BiomassPoolType { Structural = StructuralNDemand, NonStructural = NonStructuralNDemand };
            }
        }
        public override BiomassSupplyType NSupply
        {
            get
            {
            BiomassSupplyType Supply = new BiomassSupplyType();

            // Calculate Reallocation Supply.
            double _NReallocationFactor = 0;
            if (NReallocationFactor != null) //Default of zero means N reallocation is truned off
                _NReallocationFactor = NReallocationFactor.Value;
            Supply.Reallocation = SenescenceRate * StartLive.NonStructuralN * _NReallocationFactor;

            // Calculate Retranslocation Supply.
            double _NRetranslocationFactor = 0;
            if (NRetranslocationFactor != null) //Default of zero means retranslocation is turned off
                _NRetranslocationFactor = NRetranslocationFactor.Value;
            double LabileN = Math.Max(0, StartLive.NonStructuralN - StartLive.NonStructuralWt * MinimumNConc.Value);
            Supply.Retranslocation = (LabileN - StartNReallocationSupply) * _NRetranslocationFactor;

            return Supply;
            }
        }
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                Live.StructuralWt += Math.Min(value.Structural, StructuralDMDemand);

                // Excess allocation
                if (value.NonStructural < -0.0000000001)
                    throw new Exception("-ve NonStructuralDM Allocation to " + Name);
                if ((value.NonStructural - DMDemand.NonStructural) > 0.0000000001)
                    throw new Exception("StructuralDM Allocation to " + Name + " is in excess of its Capacity");
                if (DMDemand.NonStructural > 0)
                    Live.NonStructuralWt += value.NonStructural;

                // Retranslocation
                if (value.Retranslocation - StartLive.NonStructuralWt > 0.0000000001)
                    throw new Exception("Retranslocation exceeds nonstructural biomass in organ: " + Name);
                Live.NonStructuralWt -= value.Retranslocation;
            }
        }
        public override BiomassAllocationType NAllocation
        {
            set
            {
                // Allocation
                if (value.Structural > 0)
                {
                    Live.StructuralN += value.Structural;
                }
                if (value.NonStructural > 0)
                    Live.NonStructuralN += value.NonStructural;

                // Retranslocation
                if (Utility.Math.IsGreaterThan(value.Retranslocation, StartLive.NonStructuralN - StartNRetranslocationSupply))
                    throw new Exception("N Retranslocation exceeds nonstructural nitrogen in organ: " + Name);
                if (value.Retranslocation < -0.000000001)
                    throw new Exception("-ve N Retranslocation requested from " + Name);
                Live.NonStructuralN -= value.Retranslocation;

                // Reallocation
                if (Utility.Math.IsGreaterThan(value.Reallocation, StartLive.NonStructuralN))
                    throw new Exception("N Reallocation exceeds nonstructural nitrogen in organ: " + Name);
                if (value.Reallocation < -0.000000001)
                    throw new Exception("-ve N Reallocation requested from " + Name);
                Live.NonStructuralN -= value.Reallocation;

            }
        }
        public override double MaxNconc
        {
            get
            {
                return MaximumNConc.Value;
            }
        }
        public override double MinNconc
        {
            get
            {
                return MinimumNConc.Value;
            }
        }
        #endregion

    }
}