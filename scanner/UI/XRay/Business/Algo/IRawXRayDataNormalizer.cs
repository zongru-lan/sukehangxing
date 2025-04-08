using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UI.XRay.Business.Entities;

namespace UI.XRay.Business.Algo
{
    /// <summary>
    /// Interface: Normalize raw X-Ray line data for a X-Ray view.
    /// Input: A line of X-Ray data for a X-Ray view.
    /// Output: Normalized result
    /// </summary>
    public interface IRawXRayDataNormalizer
    {
        void ResetAir(ScanlineData airValue);

        void ResetGround(ScanlineData groundValue);

        /// <summary>
        /// Normalize a line of X-Ray data.
        /// </summary>
        /// <param name="rawXRayData"> raw low energy data to normalize</param>
        /// <param name="rawHe"> raw high energy data to normalize</param>
        void Normalize(ushort[] rawXRayData, ushort[] rawHe);
    }
}
