using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SVM
{
    //Coding By Sara El-Sayed El-Metwally @ Friday, April 05, 2013 9:00 pm
    // Assistant Lecturer , Faculty of Computers & Information Sciences, Mansoura University ,Eygpt.
    // Email: sarah_almetwally4@yahoo.com 
    class Attribute
    {
        private int[] __features;
        private object __cValue;
        private object __GValue;
        public Attribute(int[] _features, object _cValue, object _GValue)
        {
            __features = new int[_features.Count()];
            _features.CopyTo(__features, 0);
            this.__cValue = _cValue;
            this.__GValue = _GValue;
        }

        public object features
        {
            set
            {
                for (int i = 0; i < __features.Length; i++)
                    this.__features[i] = int.Parse(value.ToString());
            }
            get { return this.__features; }

        }
        public object cValue
        {

            set { this.__cValue = value; }
            get { return this.__cValue; }

        }
        public object Gvalue
        {

            set { this.__GValue = value; }
            get { return this.__GValue; }

        }


        // Each Attribute has a name and value for a specified object 
       /* private object __Name;
        private object __value;
        public Attribute(object _Name, object _Value)
        {
            this.__Name = _Name;
            this. __value = _Value;
        }
        public object Name
        {
            set { this.__Name = value; }
            get { return this.__Name; }
        
        }
        public object value
        {

            set { this.__value = value; }
            get { return this.__value; }
        
        }*/

    }
}
