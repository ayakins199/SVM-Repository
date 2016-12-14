using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;

namespace SVMMain
{
    //Coding By Sara El-Sayed El-Metwally @ Friday, April 05, 2013 9:00 pm
    // Assistant Lecturer , Faculty of Computers & Information Sciences, Mansoura University ,Eygpt.
    // Email: sarah_almetwally4@yahoo.com 
    class RoughSet
    {
        /*
        /// <summary>
        /// Default number of times to divide the data.
        /// </summary>
        public const int NFOLD = 10;
        /// <summary>
        /// Total number of features
        /// </summary>
        public const int NumFeatures = 17;

        // a framework of information table with a set of rough set operators
        List<Object> __Objects = new List<Object>();
        Hashtable h=new Hashtable ();
        public RoughSet(List<Object> _Objects)
        {
            this.__Objects = _Objects;
            for (int i = 0; i < _Objects.Count; i++)
            {
                Object x = _Objects[i];
                h.Add(x.Name, x.Attribute_Values);
                          
            }
           
        
        }
        public Hashtable ReturnRoughSetTable()
        {
            return h;
        
        }
        public object ReturnObjectAttributeValue(object Name,object Attribute)
        {
            object result = null;
            List<Attribute> Result = ((List<Attribute>)h[Name]);
            for (int i = 0; i < Result.Count; i++)
            {
                if (Result[i].Name == Attribute)
                {
                    result = Result[i].value;
                    break;
                }
                else
                {
                    continue;
                
                }
            
            }
            return result;
 
           
        
        }
        public string ConvertObToString(List<List<object>> Result)
        {
            string Temp = "";
            for (int f = 0; f < Result.Count; f++)
            {
                for (int j = 0; j < Result[f].Count; j++)
                {
                    Temp += Result[f][j].ToString();
                    
                }
            }


            return Temp;
        
        }
        
        private List<List<object>> ComputeDispensable_IndispensableAttributes()
        {
            List<object> Dispen = new List<object>();
            List<object> Indispen = new List<object>();
            List<object> Reducts = new List<object>();
            List<List<object>> Result = Indecernibility();
            List<List<object>> Final = new List<List<object>>();
            string S2 = ConvertObToString(Result);
            
                for (int i= 0; i < __Objects[0].__Attribute_Values.Count; i++)
                {
                    List<List<object>> Temp = Indecernibility(__Objects[0].__Attribute_Values[i].Name);
                    string S1 = ConvertObToString(Temp);
                    if (S1 == S2)
                    {
                        Dispen.Add(__Objects[0].__Attribute_Values[i].Name);
                        List<Attribute> Attr = new List<Attribute>();
                        Attr.AddRange(__Objects[0].__Attribute_Values);
                        Attr.RemoveAt(i);
                        string T = "";
                        for (int g = 0; g < Attr.Count; g++)
                        {
                            T = T+Attr[g].Name.ToString();
                      
                        }
                        Reducts.Add(T);
                        
                    }
                    else 
                    {
                      Indispen.Add(__Objects[0].__Attribute_Values[i].Name); 
                      continue; 
                    
                    }
                
                }
               
                Final.Add(Dispen);
                Final.Add(Indispen);
                Final.Add(Reducts);
                return Final;
        }
        public List<object> Dispensable()
        {
            List<List<object>> All = ComputeDispensable_IndispensableAttributes();
            return All[0];
        
        }
        public List<object> Indispensable()
        {
            List<List<object>> All = ComputeDispensable_IndispensableAttributes();
            return All[1];

        }
        public List<object> Reducts()
        {
            List<List<object>> All = ComputeDispensable_IndispensableAttributes();
            return All[2];

        }
        
        // you can exclude one of the defined attributes in the information table by providing its names/name
        // such as IND(P-{a}) the indecernibility is computed without attribute a 
        public List<List<object>> Indecernibility(object NameAttr)
        {
            List<List<object>> Result = new List<List<object>>();
            for (int i = 0; i < __Objects.Count; i++)
            {
                List<object> Temp = new List<object>();
                Temp.Add(__Objects[i].__Name);
                //j=0
                //j=i+1
                for (int j = 0; j < __Objects.Count; j++)
                {
                    if (i == j)
                        continue;
                    else
                    {
                        List<Attribute> AttVali = ((List<Attribute>)h[__Objects[i].__Name]);
                        List<Attribute> AttValj = ((List<Attribute>)h[__Objects[j].__Name]);
                        for (int f = 0; f < AttVali.Count; f++)
                        {
                            if ((AttVali[f].Name.ToString() != NameAttr.ToString()) || (AttVali[f].Name.ToString() != NameAttr.ToString()))
                            {
                                if (AttVali[f].value.ToString() == AttValj[f].value.ToString())
                                {
                                    if (f == AttVali.Count - 1)
                                    {
                                        
                                        Temp.Add(__Objects[j].__Name);
                                        

                                    }//Reach the Last Attribute
                                    else
                                    {
                                        continue;
                                    }


                                }// If Attributes_Values are Equal 
                                else
                                {
                                    break;
                                }// If Attributes_Values are Not Equal
                            }//For Remove Specified Attribute From Indecirnibility
                            else
                            {
                                if (f == AttVali.Count - 1)
                                {
                                    Temp.Add(__Objects[j].__Name);
                                }//When we Remove Final Attribute 
                                continue;
                            
                            }


                        }//Loop to Search All Atributes 



                    }//Else To Not Compare each item with itself 


                }//For Jitems

                if (Result.Count != 0)
                {
                    if(checkDuplicate(Result, Temp) == false)
                        Result.Add(Temp);
                }
                else
                Result.Add(Temp);
            }// For Iitems
            return Result;
        }
        // Compute Indecernibility under all defined attributes AT in the information table "Table"
        public List<List<object>> Indecernibility()
        {
            List<List<object>> Result = new List<List<object>>();
            for (int i = 0;i< __Objects.Count; i++)
            {
                List<object> Temp = new List<object>();
                Temp.Add(__Objects[i].__Name);
                
                for (int j = 0; j < __Objects.Count; j++)
                {
                    if (i == j)
                        continue;
                    else
                    {
                        List<Attribute> AttVali = ((List<Attribute>)h[__Objects[i].__Name]);
                        List<Attribute> AttValj = ((List<Attribute>)h[__Objects[j].__Name]);
                        for (int f = 0; f < AttVali.Count; f++)
                        {
                            if (AttVali[f].value.ToString() == AttValj[f].value.ToString())
                            {
                                if (f == AttVali.Count - 1)
                                {
                                   
                                    Temp.Add(__Objects[j].__Name);
                                    

                                }//Reach the Last Attribute
                                else
                                {
                                    continue;
                                }


                            }// If Attributes_Values are Equal 
                            else
                            {
                                break;
                            }// If Attributes_Values are Not Equal 
                        
                        
                        }//Loop to Search All Atributes 



                    }//Else To Not Compare each item with itself 
                
                
                }//For Jitems

                //Dont add the same set of elements that has been added before
                if (Result.Count != 0) //if count equals zero, then no element has been added
                {
                    if(checkDuplicate(Result,Temp) ==  false)
                        Result.Add(Temp); //add the new set if it has not been added before
                    //bool addAttribute = false;
                    //for (int m = 0; m < Result.Count; m++)
                    //{
                    //    for (int n = 0; n < Temp.Count; n++)
                    //    {
                    //        if (Result[m].Contains(Temp[n]))
                    //        {
                    //            addAttribute = true;
                    //            break;
                    //        }
                    //    }
                    //    if (m == (Result.Count - 1) && addAttribute == false)
                    //        Result.Add(Temp);
                    //}
                }
                else
                    Result.Add(Temp); //add the first set of attributes (equivalent classes) 
                
            }// For Iitems
            return Result;
        }

        //this method ensures that no indispensable element is repeated in any set
        public bool checkDuplicate(List<List<object>> Result, List<object> Temp)
        {
            bool addAttribute = false;
            for (int m = 0; m < Result.Count; m++)
            {
                for (int n = 0; n < Temp.Count; n++)
                {
                    if (Result[m].Contains(Temp[n]))
                    {
                        addAttribute = true;
                        break;
                    }
                }
                //if (m == (Result.Count - 1) && addAttribute == false)
                //    Result.Add(Temp);
            }
            return addAttribute;
        }

        //calculate the dependency of each attribute - i.e IND(A-{a},A), the dependency of IND(A) on IND(A-{a}) A refers to all the attributes
        public double[] computeDependency(Dictionary<char, string> allAttributeIndecirnibleSet)
        {
            int attrCount = allAttributeIndecirnibleSet.Count; //attribute count
            string[] splitAllAttr = allAttributeIndecirnibleSet.Values.ElementAt(0).Split(new string[] { "{", "}" }, StringSplitOptions.None); //split the set for IND(A)
            splitAllAttr = splitAllAttr.Where(c => c != "").ToArray(); //remove the empty rows
            double[] attrDepedency = new double[attrCount - 1]; //attribute dependency

            for (int j = 1; j < attrCount; j++) //start from the second row (i.e j=1), since the first row contains IND(A)
            {
                int count = 0;
                string[] splitEachAttr = allAttributeIndecirnibleSet.Values.ElementAt(j).Split(new string[] { "{", "}" }, StringSplitOptions.None);
                splitEachAttr = splitEachAttr.Where(c => c != "").ToArray(); //remove the empty rows

                for (int k = 0; k < splitEachAttr.Count(); k++)
                {
                    for (int l = 0; l < splitAllAttr.Count(); l++)
                    {
                        if (splitEachAttr[k] == splitAllAttr[l])
                            count++;
                    }
                }

                attrDepedency[j - 1] = Math.Round(((double)count / (double)splitAllAttr.Count()) * 100, 3); //calculating the dependency for all the attributes and round off to 3 DP
            }

            return attrDepedency;
        }

        //this function compute the set of reducts - i.e the subset of the original set of features
        public string computeReductSet(List<Object> elments_of_Universe)
        {
            string trainTestDataPath = String.Format(Environment.CurrentDirectory + "\\{0}", "TrainTestData");
            string[] trainTestFileURL = System.IO.Directory.GetFileSystemEntries(trainTestDataPath);
            string[] setOfReducts = new string[NFOLD];
            string reduct = "";
            Program p = new Program();
            
                //this.Prepare_Data_Table(groupedSet);
                //RoughSet RouObj = new RoughSet(this.elments_of_Universe);
                List<object> Indispen = this.Indispensable();
                List<List<object>> Indecirnibility = this.Indecernibility();
                int allAttrIndCount = Indecirnibility.Count; //the count of the indiscirnibility for all the attributes (i.e IND(A))

                int attributeCount = elments_of_Universe[0].Attribute_Values.Count; //get the total number of attributes or features
                char[] attributeLabels = new char[attributeCount];
                //extract all the attributes and save them in attributeLabels
                for (int i = 0; i < 1; i++)
                {
                    for (int j = 0; j < attributeCount; j++)
                    {
                        attributeLabels[j] = (char)elments_of_Universe[i].Attribute_Values[j].Name;
                    }

                }
                Dictionary<char, string> allAttributeIndecirnibleSet = new Dictionary<char, string>();
                List<List<object>> eachAttrIndecirnibility = new List<List<object>>();
                double[] dependency = new double[attributeLabels.Count()];
                
                string eachGroupedSet = "", allGroupedSet = "";

                //collating the Indecirnibility for the the whole attribute (i.e the dataset)
                for (int i = 0; i < 1; i++)
                {
                    for (int j = 0; j < Indecirnibility.Count; j++)
                    {
                        for (int k = 0; k < Indecirnibility[j].Count; k++)
                            eachGroupedSet += string.Format("{0} ", Indecirnibility[j][k]);

                        eachGroupedSet = string.Format("{0}{1}{2}", "{", eachGroupedSet.TrimEnd(' '), "}");
                        allGroupedSet += string.Format("{0}", eachGroupedSet); eachGroupedSet = "";
                    }

                    allAttributeIndecirnibleSet['A'] = allGroupedSet; //'A' is used to represent the key for the collated Indecirnibility of the dataset
                }

                //collating the Indecirnibility for each attribute
                for (int i = 0; i < attributeLabels.Count(); i++)
                {
                    allGroupedSet = "";
                    eachAttrIndecirnibility = this.Indecernibility(attributeLabels[i]);
                    dependency[i] = ((double)eachAttrIndecirnibility.Count / (double)allAttrIndCount) * 100; //depedency = IND(A-{attr})/IND(A)
                    for (int j = 0; j < eachAttrIndecirnibility.Count; j++)
                    {
                        for (int k = 0; k < eachAttrIndecirnibility[j].Count; k++)
                            eachGroupedSet += string.Format("{0} ", eachAttrIndecirnibility[j][k]); //collating the individual elements in each set

                        eachGroupedSet = string.Format("{0}{1}{2}", "{", eachGroupedSet.TrimEnd(' '), "}");
                        allGroupedSet += string.Format("{0}", eachGroupedSet); eachGroupedSet = ""; //collating each group of elements together
                    }
                    allAttributeIndecirnibleSet[attributeLabels[i]] = allGroupedSet; //saving each collated group of elements and their equivalent classes
                }

                //computing the dependency for each attribute (i.e the dependency of IND(A) on IND(A-{a}) )
                double[] attrDependency = this.computeDependency(allAttributeIndecirnibleSet);
                
                for (int i = 0; i < attrDependency.Count(); i++)
                {
                    if (attrDependency[i] != 100)
                        reduct += string.Format(" {0}", i);
                }
                //setOfReducts[a] = reduct;
            
            return reduct;
        }*/
    }
}
