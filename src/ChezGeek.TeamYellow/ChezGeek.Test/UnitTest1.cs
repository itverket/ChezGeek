using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ChezGeek.TeamYellow.Actors;
using ChezGeek.TeamYellow.Messages;
using ChezGeek.TeamYellow.Services;
using Geek2k16.Entities;
using Geek2k16.Entities.Enums;
using Geek2k16.Entities.Structs;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace ChezGeek.Test
{
    [TestClass]
    public class UnitTest1
    {
        private YellowCalculationService _calculationService = new YellowCalculationService();

        [TestMethod]
        public void TestMethod1()
        {

            // Arrange
            var initialGrid = new Abbr?[,]
            {
                {Abbr.BR, Abbr.BN, Abbr.BB, Abbr.BQ, Abbr.BK, Abbr.BB, Abbr.BN, Abbr.BR},
                {Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {Abbr.WP, Abbr.WP, Abbr.WP, Abbr.WP, Abbr.WP, Abbr.WP, Abbr.WP, Abbr.WP},
                {Abbr.WR, Abbr.WN, Abbr.WB, Abbr.WQ, Abbr.WK, Abbr.WB, Abbr.WN, Abbr.WR}
            }; 
            var initialState = _calculationService.GetStateFromGrid(initialGrid);

            var newGrid = new Abbr?[,]
            {
                {Abbr.BR, Abbr.BN, Abbr.BB, Abbr.BQ, Abbr.BK, Abbr.BB, Abbr.BN, Abbr.BR},
                {Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP, Abbr.BP},
                {null, null, null, null, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {null, null, null, Abbr.WP, null, null, null, null},
                {null, null, null, null, null, null, null, null},
                {Abbr.WP, Abbr.WP, Abbr.WP, null, Abbr.WP, Abbr.WP, Abbr.WP, Abbr.WP},
                {Abbr.WR, Abbr.WN, Abbr.WB, Abbr.WQ, Abbr.WK, Abbr.WB, Abbr.WN, Abbr.WR}
            };
            var newState = _calculationService.GetStateFromGrid(newGrid);

            // Act
            var initialValue = _calculationService.GetValueOfPieces(initialState);
            var newValue = _calculationService.GetValueOfPieces(newState);


            // Assert
            Assert.IsTrue(newValue > initialValue);
        }
    }
}
