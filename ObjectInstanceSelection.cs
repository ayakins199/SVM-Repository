using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SVM
{
    public class ObjectInstanceSelection
    {
        // Each object has a name with set of defined attribute-values
        public double __cValue;
        public double __GValue;
        public int[] __Attribute_Values;
        public double[] __Attribute_Values_Continuous;
        public int[] __Pointers;
        public double __Fitness;
        public double __Frequency;
        public double[] __Velocity;
        public int __NodePointer;
        public double __PheromoneValue;
        public double __InitialPheromoneValue;
        public double __HeuristicInformation;
        public double __Position;

        //constructor for firefly algorithm
        public ObjectInstanceSelection(double _cValue, double _GValue, int[] _Attribute_Values, int[] _Pointers)
        {
            __Attribute_Values = new int[_Attribute_Values.Count()];
            __Pointers = new int[_Pointers.Count()];
            this.__cValue = _cValue;
            this.__GValue = _GValue;
            _Attribute_Values.CopyTo(__Attribute_Values, 0);
            _Pointers.CopyTo(__Pointers, 0);
        }

        //constructor for FPA and Cuckoo Algorithm
        public ObjectInstanceSelection(int[] _Attribute_Values, double[] _Attribute_Values_Continuous, int[] _Pointers, double _fitness)
        {
            __Attribute_Values = new int[_Attribute_Values.Count()];
            __Attribute_Values_Continuous = new double[_Attribute_Values_Continuous.Count()];
            __Pointers = new int[_Pointers.Count()];
            _Attribute_Values.CopyTo(__Attribute_Values, 0);
            _Attribute_Values_Continuous.CopyTo(__Attribute_Values_Continuous, 0);
            _Pointers.CopyTo(__Pointers, 0);
            this.__Fitness = _fitness;
        }

        //constructor for Social Spider Algorithm
        public ObjectInstanceSelection(int[] _Attribute_Values, double[] _Attribute_Values_Continuous, int[] _Pointers, double _fitness, double _position)
        {
            __Attribute_Values = new int[_Attribute_Values.Count()];
            __Attribute_Values_Continuous = new double[_Attribute_Values_Continuous.Count()];
            __Pointers = new int[_Pointers.Count()];
            _Attribute_Values.CopyTo(__Attribute_Values, 0);
            _Attribute_Values_Continuous.CopyTo(__Attribute_Values_Continuous, 0);
            _Pointers.CopyTo(__Pointers, 0);
            this.__Fitness = _fitness;
            this.__Position = _position;
        }

        //constructor for bat algorithm
        public ObjectInstanceSelection(int[] _Attribute_Values, double[] _Attribute_Values_Continuous, double _Frequency, double[] _Velocity, int[] _Pointers, double _fitness)
        {
            __Attribute_Values = new int[_Attribute_Values.Count()];
            __Attribute_Values_Continuous = new double[_Attribute_Values_Continuous.Count()];
            __Pointers = new int[_Pointers.Count()];
            __Velocity = new double[_Velocity.Count()];
            _Attribute_Values.CopyTo(__Attribute_Values, 0);
            _Attribute_Values_Continuous.CopyTo(__Attribute_Values_Continuous, 0);
            _Pointers.CopyTo(__Pointers, 0);
            _Velocity.CopyTo(__Velocity, 0);
            this.__Frequency = _Frequency;
            this.__Fitness = _fitness;
        }

        //constructor for ACO algorithm
        public ObjectInstanceSelection(int _NodePointer, double _InitialPheromoneValue, double _PheromoneValue, double _HeuristicInformation, double _Position, int[] _Pointers)
        {
            this.__NodePointer = _NodePointer;
            this.__InitialPheromoneValue = _InitialPheromoneValue;
            this.__PheromoneValue = _PheromoneValue;
            this.__HeuristicInformation = _HeuristicInformation;
            this.__Position = _Position;
            __Pointers = new int[_Pointers.Count()];
            _Pointers.CopyTo(__Pointers, 0);
        }

        public int NodePointer
        {
            set { this.__NodePointer = int.Parse(value.ToString()); }
            get { return this.__NodePointer; }
        }

        public double InitialPheromoneValue
        {
            set { this.__InitialPheromoneValue = double.Parse(value.ToString()); }
            get { return this.__InitialPheromoneValue; }
        }

        public double PheromoneValue
        {
            set { this.__PheromoneValue = double.Parse(value.ToString()); }
            get { return this.__PheromoneValue; }
        }

        public double HeuristicInformation
        {
            set { this.__HeuristicInformation = double.Parse(value.ToString()); }
            get { return this.__HeuristicInformation; }
        }

        public double Position
        {
            set { this.__Position = double.Parse(value.ToString()); }
            get { return this.__Position; }
        }

        public object cValue
        {
            set { this.__cValue = double.Parse(value.ToString()); }
            get { return this.__cValue; }

        }
        public object GValue
        {
            set { this.__GValue = double.Parse(value.ToString()); }
            get { return this.__GValue; }

        }
        public int[] Attribute_Values
        {
            set { this.__Attribute_Values = value; }
            get { return this.__Attribute_Values; }
        }
        public double[] Attribute_Values_Continuous
        {
            set { this.__Attribute_Values_Continuous = value; }
            get { return this.__Attribute_Values_Continuous; }

        }
        public int[] Pointers
        {
            set { this.__Pointers = value; }
            get { return this.__Pointers; }

        }
        public double Fitness
        {
            set { this.__Fitness = double.Parse(value.ToString()); }
            get { return this.__Fitness; }

        }
        public double Frequency
        {
            set { this.__Frequency = value; }
            get { return this.__Frequency; }
        }
        public double[] Velocity
        {
            set { this.__Velocity = value; }
            get { return this.__Velocity; }
        }
    }
}
