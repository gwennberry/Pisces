﻿using System;
using System.Collections.Generic;

namespace CallSomaticVariants.Models
{
    public class AlignmentSet
    {
        public Read PartnerRead1 { get; private set; }
        public Read PartnerRead2 { get; private set; }

        public List<Read> ReadsForProcessing { get; set; }
        public bool IsStitched { get; set; }

        //Maybe add something like this or other validation logic to make sure there aren't somehow more than 2 buddies that try to get in here
        public bool IsFullPair
        {
            get { return PartnerRead1 != null && PartnerRead2 != null; }
        }

        public AlignmentSet(Read read1, Read read2, bool readyToProcess = false)
        {
            if (read1 == null)
                throw new ArgumentException("Read 1 cannot be null.");

            if (read2 == null)
                PartnerRead1 = read1;
            else
            {
                var read1AdjustedPosition = read1.Position - (read1.CigarData == null ? 0 : read1.CigarData.GetPrefixClip());
                var read2AdjustedPosition = read2.Position - (read2.CigarData == null ? 0 : read2.CigarData.GetPrefixClip());

                if (read1AdjustedPosition > read2AdjustedPosition)
                {
                    var tmpRead = read2;
                    read2 = read1;
                    read1 = tmpRead;
                }
                PartnerRead1 = read1;
                PartnerRead2 = read2;
            }

            ReadsForProcessing = new List<Read>();

            if (readyToProcess)
            {
                ReadsForProcessing.Add(PartnerRead1);
                if (PartnerRead2 != null)
                    ReadsForProcessing.Add(PartnerRead2);
            }
        }
    }
}