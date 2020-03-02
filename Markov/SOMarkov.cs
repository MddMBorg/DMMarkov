using System.Collections.Generic;

namespace Markov
{
    public class SOMarkov
    {
        public string Base;
        public string Base2;
        public string Next;
        public int Count = 0;
        public double Probability = 0;
        public double Weight = 0;
        public List<double> Weights = new List<double>();

    }

}