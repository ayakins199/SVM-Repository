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
    //this class combines the three parts of each firefly (i.e. the feature mask, CValue and Gamma) into one object
    public class Object
    {
        // Each object has a name with set of defined attribute-values
        public double __cValue;
        public double __GValue;
        public int[] __Attribute_Values;

        public Object(double _cValue, double _GValue, int[] _Attribute_Values)
        {
            __Attribute_Values = new int[_Attribute_Values.Count()];
            this.__cValue = _cValue;
            this.__GValue = _GValue;
            _Attribute_Values.CopyTo(__Attribute_Values, 0);
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
        


        /*public object __Name;
        public List<Attribute> __Attribute_Values = new List<Attribute>();
       

        public Object(object _Name, List<Attribute> _Attribute_Values)
        {
            this.__Name = _Name;
            this.__Attribute_Values = _Attribute_Values;
            
        }
        public object Name
        {
            set { this.__Name = value; }
            get { return this.__Name; }
        
        }
        public List<Attribute> Attribute_Values
        {
            set { this.__Attribute_Values = value; }
            get { return this.__Attribute_Values; }


        }
        
        public object this[object Name]
        {
            set
            {
                for (int i = 0; i < __Attribute_Values.Count; i++)
                {
                    Attribute x = __Attribute_Values[i];
                    if (x.Name == Name)
                    {
                        x.value = value;
                        break;
                    }
                    else
                    { continue; }

                }
            }
            get
            {
                object Val = null;
                for (int i = 0; i < __Attribute_Values.Count; i++)
                {
                    Attribute x = __Attribute_Values[i];
                    if (x.Name == Name)
                    {
                        Val = x.value;
                        break;
                    }
                    else
                    { continue; }
                }
                return Val;
            }
        




        }*/
    }
}
