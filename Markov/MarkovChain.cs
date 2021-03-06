﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetLearn.Markov
{
    [Serializable]
    [ProtoContract]
    public class MarkovChain<T> where T : IEquatable<T>
    {

        [ProtoMember(1)]
        Dictionary<ChainState<T>, Dictionary<T, int>> frequencyTable;


        [ProtoMember(2)]
        public int Order { get; private set; }
     
        public MarkovChain()
        {
            this.Order = 2;
            this.frequencyTable = new Dictionary<ChainState<T>, Dictionary<T, int>>();
        }
   
        public MarkovChain(int order)
        {
            this.Order = order; 
            this.frequencyTable = new Dictionary<ChainState<T>, Dictionary<T, int>>();
        }

        public T[] GetPossibleItemClasses()
        {
            Dictionary<T,int> dic = new Dictionary<T,int>();
            
            foreach(var freqs in frequencyTable.Values)
            {
                foreach (var k in freqs.Keys)
                    dic[k] = 0;
            }

            return dic.Keys.ToArray();
        }

        public double EvaluateLogProbability(T[] observationSequence)
        {
             double logMinProb = Math.Log(0.000001);

            double logProb = Math.Log(1);
            var states = GetChainStates(observationSequence);
            int j = this.Order;


            foreach(var s in states)
            {
                var nextNote = observationSequence[j++];

                if (j >= observationSequence.Length)
                    break;

                if(!frequencyTable.ContainsKey(s))
                {
                    logProb += logMinProb;
                    continue;
                }
            
                var freqs = frequencyTable[s];
                double sum = 0;
                int[] values = freqs.Values.ToArray();
                for (int i = 0; i < values.Length; i++)
                {
                    sum += values[i];
                }
                if (!freqs.ContainsKey(nextNote))
                    logProb += logMinProb;
                else
                    logProb += Math.Log(freqs[nextNote] / sum);
            }

            return logProb;

        }

        private ChainState<T>[] GetChainStates(T[] observationSequence)
        {
            List<ChainState<T>> states = new List<ChainState<T>>();

            for(int i = this.Order; i < observationSequence.Length; i++)
            {
                T[] items = new T[this.Order];
                for(int j = i - this.Order; j < i; j++)
                {
                    items[this.Order - (i - j)] = observationSequence[j];
                }
                ChainState<T> state = new ChainState<T>(items);
                states.Add(state);
            }
            T[] items2 = new T[this.Order]; int k = 0;
            for(int i = observationSequence.Length-this.Order; i < observationSequence.Length; i++)
            {
                items2[k++] = observationSequence[i];
            }
            states.Add(new ChainState<T>(items2));
            return states.ToArray();
        }

        public void Add(IEnumerable<T> items)
        {
            //int i = 0;

            List<T> items_list = new List<T>();

            foreach (var it in items)
                items_list.Add(it);

            T[] items_arr = items_list.ToArray();
            for (int i = Order; i < items_arr.Length; i++ )
            {
     
                T[] temp_arr = new T[Order];
                for(int j = Order; j > 0; j--)
                {
                    temp_arr[Order-j] = items_arr[i - (j)];
                    //key.Add(items_arr[i - (j)]);
                }
                ChainState<T> key = new ChainState<T>(temp_arr);

                var next_item = items_arr[i];


                if (frequencyTable.ContainsKey(key))
                {
                }
                else
                {
                    frequencyTable[key] = new Dictionary<T, int>();
                }
                if (frequencyTable[key].ContainsKey(next_item))
                {
                    frequencyTable[key][next_item]++;
                }
                else
                {
                    frequencyTable[key][next_item] = 1;
                }

            }

        }

        /// <summary>
        /// Returns a random item according to the proportions
        /// </summary>
        /// <typeparam name="B"></typeparam>
        /// <param name="items">Array of values</param>
        /// <param name="proportions">Weight of each item</param>
        /// <param name="rand">Uses own random if none is given</param>
        /// <returns></returns>
        public static B RouletteSelection<B>(B[] items, double[] proportions, Random rand=null)
        {
     
            if (items.Length != proportions.Length)
                throw new Exception("WTP length mismatch");

            double total_freq = 0;
            foreach (var freq in proportions)
                total_freq += freq;

            // Cumulative probabilities
            double[] probabilities_cum = new double[proportions.Length];
            for (int i = 0; i < items.Length; i++ )
            {
                double proportion = proportions[i] / total_freq;

                if (i == 0)
                    probabilities_cum[i] = proportion;
                else
                    probabilities_cum[i] = probabilities_cum[i - 1] + proportion;
            }

            double randomDouble = (double)(rand == null ? StaticRandom.NextDouble() : rand.NextDouble());
            double random_prob = (double)(probabilities_cum[probabilities_cum.Length - 1] * randomDouble);
            int index = Array.BinarySearch<double>(probabilities_cum, random_prob);
            if (index < 0)
                index = -(index + 1);
            if (index == items.Length)
                index = items.Length - 1;
            return items[index];
        }

        public T[] Chain(int MAX_LENGTH = 100, int seed=0)
        {
            Random random = new Random(seed);

            var keys = frequencyTable.Keys.ToArray();
            var randomKey = keys[random.Next(0, keys.Length)];
            return Chain(randomKey.GetItems(), MAX_LENGTH, seed);
        }

        public T[] Chain(T[] start, int MAX_LENGTH = 100, int seed=0)
        {
            
            List<T> ze_items = new List<T>();
            Queue<T> last_words = new Queue<T>();

            int max_key_items = Order;
            foreach (var item in start)
            {
                ze_items.Add(item);
                if (max_key_items > 0)
                {
                    last_words.Enqueue(item);
                    max_key_items--;
                }
            }

            int iterations = 0;

            Random random = new Random(seed);

            while (iterations < MAX_LENGTH)
            {

                ChainState<T> item_key = new ChainState<T>(last_words);

                if (!frequencyTable.ContainsKey(item_key))
                    break;

                T[] words_arr = frequencyTable[item_key].Keys.ToArray();

                double[] frequencies = new double[words_arr.Length];

                int k = 0;
                foreach (var next_word in words_arr)
                {
                    frequencies[k++] = frequencyTable[item_key][next_word];
                }

                T next_word_ = RouletteSelection<T>(words_arr, frequencies, random);
                ze_items.Add(next_word_);

                last_words.Dequeue();
                last_words.Enqueue(next_word_);

                iterations++;
            }
            
            return ze_items.ToArray();
        }
    }
}


/*Complete start
i<1000
--
His
Time 5142
Length 147110

Mine
Time 2871
Length 201000

--
Declaration done
i<10000
--
His
Time  10075
Length 5 075 135

Mine
Time 10054
Length 2 210 000
*/