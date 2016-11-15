using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChezGeek.TeamPurple.Openings
{
    internal class OpeningFactory
    {
        Dictionary<int, IOpening> _availableOpenings;
        Random _random;
        public OpeningFactory()
        {
            _random = new Random();

            _availableOpenings = new Dictionary<int, IOpening>();
            //_availableOpenings.Add(_availableOpenings.Count, new SpanishOpening());
            //_availableOpenings.Add(_availableOpenings.Count, new BerlinWallOpening());
            _availableOpenings.Add(_availableOpenings.Count, new TwoNightsDefenceOpening());
            _availableOpenings.Add(_availableOpenings.Count, new SicilianDefence());

        }
        internal IOpening GetOpening()
        {
            var randomIndex = _random.Next(0, _availableOpenings.Count);
            return _availableOpenings[randomIndex];
        }

    }

    
}
