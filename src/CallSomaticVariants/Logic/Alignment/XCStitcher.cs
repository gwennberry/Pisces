﻿using System;
using System.Linq;
using System.Text;
using CallSomaticVariants.Infrastructure;
using CallSomaticVariants.Interfaces;
using CallSomaticVariants.Models;
using CallSomaticVariants.Types;
using CallSomaticVariants.Utility;
using SequencingFiles;

namespace CallSomaticVariants.Logic.Alignment
{
    public class XCStitcher : BaseStitcher
    {
        public XCStitcher(int minBaseCallQuality) : base(minBaseCallQuality)
        {
        }

        public override void TryStitch(AlignmentSet set)
        {
            if (set.PartnerRead1 == null || set.PartnerRead2 == null)
                throw new ArgumentException("Set has missing read.");

            if (set.PartnerRead1.Chromosome != set.PartnerRead2.Chromosome)
                throw new ArgumentException("Partner reads are from different chromosomes.");

            // this logic relies on read1 being before read2, 
            // which should already be enforced in the alignmentset.

            try
            {
                // Get the stitched cigar. 
                var stitchedCigar = GetStitchedCigar(set);

                var overlapBoundary = GetOverlapBoundary(set.PartnerRead1, set.PartnerRead2, stitchedCigar.ToString());

                var mergedRead = GenerateConsensus(set.PartnerRead1, set.PartnerRead2, stitchedCigar, overlapBoundary);

                set.ReadsForProcessing.Add(mergedRead);

            }
            catch (Exception ex)
            {
                //keep going but don't try to merge
                if (ex is ReadsNotStitchableException)
                {
                    throw;
                }
                var wrappedException = new Exception("Error stitching reads: " + ex.Message, ex);
                Logger.WriteExceptionToLog(wrappedException);

            }
        }

        // get overlap boundary relative to reads 
        // first key is read1 index position that is the start of the overlap
        // second key is read2 index position that is the end of the overlap
        public static OverlapBoundary GetOverlapBoundary(Read read1, Read read2, string stitchedCigar)
        {
            var totalStitchedLength = new CigarAlignment(stitchedCigar).GetReadSpan();

            var overlapLength = read1.Sequence.Length + read2.Sequence.Length - (int)totalStitchedLength;

            if (overlapLength <= 0)
                throw new ReadsNotStitchableException(string.Format("No overlap between reads {0} and {1}", read1.ToString(), read2.ToString()));

            //In this case, we'll just assume that the stitching is simple and the overlap reaches exactly as far back into R1 as it does forward into R2.
            var overlapBoundary = new OverlapBoundary()
            {
                OverlapLength = overlapLength,
                Read1 = new ReadIndexBoundary()
                {
                    StartIndex = read1.Sequence.Length - overlapLength,
                    EndIndex = read1.Sequence.Length - 1
                },
                Read2 = new ReadIndexBoundary()
                {
                    StartIndex = 0,
                    EndIndex = overlapLength - 1
                }
            };

            return overlapBoundary;
        }


        private CigarAlignment GetStitchedCigar(AlignmentSet set)
        {
            // preferentially take XC tag if available
            if (set.PartnerRead1.StitchedCigar != null &&
                set.PartnerRead2.StitchedCigar != null && set.PartnerRead1.StitchedCigar.ToString() == set.PartnerRead2.StitchedCigar.ToString())

                return set.PartnerRead1.StitchedCigar;

            throw new ReadsNotStitchableException(string.Format("XC tag is not available for reads {0} and {1}", set.PartnerRead1.ToString(), set.PartnerRead2.ToString()));
        }

    }
}