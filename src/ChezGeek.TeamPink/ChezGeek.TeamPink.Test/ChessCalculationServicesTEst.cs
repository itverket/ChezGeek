using System;
using Geek2k16.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChezGeek.TeamPink.Test
{
    [TestClass]
    public class ChessCalculationServicesTest
    {
        [TestMethod]
        public void TestMethod1()
        {

            var service = new ChessCalculationsService();
            var initial = service.GetInitialState();
            
            var score = service.GetValueOfPieces(initial.ChessGrid);

            Assert.AreEqual(0, score);
        }
    }
}
