/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Chummer.Annotations;
using Chummer.Backend.Equipment;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace Chummer
{
    public static class CommonFunctions
    {
        #region XPath Evaluators

        // TODO: implement a sane expression evaluator
        // A single instance of an XmlDocument and its corresponding XPathNavigator helps reduce overhead of evaluating XPaths that just contain mathematical operations
        private static readonly XmlDocument s_ObjXPathNavigatorDocument = new XmlDocument {XmlResolver = null};

        private static readonly XPathNavigator s_ObjXPathNavigator = s_ObjXPathNavigatorDocument.CreateNavigator();

        private static readonly ConcurrentDictionary<string, Tuple<bool, object>> s_DicCompiledEvaluations =
            new ConcurrentDictionary<string, Tuple<bool, object>>();

        private static readonly char[] s_LstInvariantXPathLegalChars = "1234567890+-*abdegilmnortuv()[]{}!=<>&;. ".ToCharArray();

        /// <summary>
        /// Evaluate a string consisting of an XPath Expression that could be evaluated on an empty document.
        /// </summary>
        /// <param name="strXPath">String as XPath Expression to evaluate.</param>
        /// <returns>System.Boolean, System.Double, System.String, or System.Xml.XPath.XPathNodeIterator depending on the result type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object EvaluateInvariantXPath(string strXPath)
        {
            if (s_DicCompiledEvaluations.TryGetValue(strXPath, out Tuple<bool, object> objCachedEvaluation))
            {
                return objCachedEvaluation.Item2;
            }

            if (string.IsNullOrWhiteSpace(strXPath))
            {
                s_DicCompiledEvaluations.TryAdd(strXPath, null);
                return null;
            }

            if (!strXPath.IsLegalCharsOnly(true, s_LstInvariantXPathLegalChars))
            {
                s_DicCompiledEvaluations.TryAdd(strXPath, new Tuple<bool, object>(false, strXPath));
                return strXPath;
            }

            object objReturn;
            bool blnIsSuccess;
            try
            {
                objReturn = s_ObjXPathNavigator.Evaluate(strXPath.TrimStart('+'));
                blnIsSuccess = true;
            }
            catch (ArgumentException)
            {
                Utils.BreakIfDebug();
                objReturn = strXPath;
                blnIsSuccess = false;
            }
            catch (XPathException)
            {
                Utils.BreakIfDebug();
                objReturn = strXPath;
                blnIsSuccess = false;
            }
            s_DicCompiledEvaluations.TryAdd(strXPath, new Tuple<bool, object>(blnIsSuccess, objReturn)); // don't want to store managed objects, only primitives
            return objReturn;
        }

        /// <summary>
        /// Evaluate a string consisting of an XPath Expression that could be evaluated on an empty document.
        /// </summary>
        /// <param name="strXPath">String as XPath Expression to evaluate.</param>
        /// <param name="blnIsSuccess">Whether we successfully processed the XPath (true) or encountered an error (false).</param>
        /// <returns>System.Boolean, System.Double, System.String, or System.Xml.XPath.XPathNodeIterator depending on the result type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object EvaluateInvariantXPath(string strXPath, out bool blnIsSuccess)
        {
            if (s_DicCompiledEvaluations.TryGetValue(strXPath, out Tuple<bool, object> objCachedEvaluation))
            {
                blnIsSuccess = objCachedEvaluation.Item1;
                return objCachedEvaluation.Item2;
            }

            if (string.IsNullOrWhiteSpace(strXPath))
            {
                s_DicCompiledEvaluations.TryAdd(strXPath, new Tuple<bool, object>(false, null));
                blnIsSuccess = false;
                return null;
            }

            if (!strXPath.IsLegalCharsOnly(true, s_LstInvariantXPathLegalChars))
            {
                s_DicCompiledEvaluations.TryAdd(strXPath, new Tuple<bool, object>(false, strXPath));
                blnIsSuccess = false;
                return strXPath;
            }

            object objReturn;
            try
            {
                objReturn = s_ObjXPathNavigator.Evaluate(strXPath.TrimStart('+'));
                blnIsSuccess = true;
            }
            catch (ArgumentException)
            {
                Utils.BreakIfDebug();
                objReturn = strXPath;
                blnIsSuccess = false;
            }
            catch (XPathException)
            {
                Utils.BreakIfDebug();
                objReturn = strXPath;
                blnIsSuccess = false;
            }
            s_DicCompiledEvaluations.TryAdd(strXPath, new Tuple<bool, object>(blnIsSuccess, objReturn)); // don't want to store managed objects, only primitives
            return objReturn;
        }

        /// <summary>
        /// Evaluate an XPath Expression that could be evaluated on an empty document.
        /// </summary>
        /// <param name="objXPath">XPath Expression to evaluate</param>
        /// <returns>System.Boolean, System.Double, System.String, or System.Xml.XPath.XPathNodeIterator depending on the result type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object EvaluateInvariantXPath(XPathExpression objXPath)
        {
            string strExpression = objXPath.Expression;
            if (s_DicCompiledEvaluations.TryGetValue(strExpression, out Tuple<bool, object> objCachedEvaluation))
            {
                return objCachedEvaluation.Item2;
            }

            object objReturn;
            bool blnIsSuccess;
            try
            {
                objReturn = s_ObjXPathNavigator.Evaluate(objXPath);
                blnIsSuccess = true;
            }
            catch (ArgumentException)
            {
                Utils.BreakIfDebug();
                objReturn = strExpression;
                blnIsSuccess = false;
            }
            catch (XPathException)
            {
                Utils.BreakIfDebug();
                objReturn = strExpression;
                blnIsSuccess = false;
            }
            s_DicCompiledEvaluations.TryAdd(strExpression, new Tuple<bool, object>(blnIsSuccess, objReturn)); // don't want to store managed objects, only primitives
            return objReturn;
        }

        /// <summary>
        /// Evaluate an XPath Expression that could be evaluated on an empty document.
        /// </summary>
        /// <param name="objXPath">XPath Expression to evaluate</param>
        /// <param name="blnIsSuccess">Whether we successfully processed the XPath (true) or encountered an error (false).</param>
        /// <returns>System.Boolean, System.Double, System.String, or System.Xml.XPath.XPathNodeIterator depending on the result type.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object EvaluateInvariantXPath(XPathExpression objXPath, out bool blnIsSuccess)
        {
            string strExpression = objXPath.Expression;
            if (s_DicCompiledEvaluations.TryGetValue(strExpression, out Tuple<bool, object> objCachedEvaluation))
            {
                blnIsSuccess = objCachedEvaluation.Item1;
                return objCachedEvaluation.Item2;
            }

            object objReturn;
            try
            {
                objReturn = s_ObjXPathNavigator.Evaluate(objXPath);
                blnIsSuccess = true;
            }
            catch (ArgumentException)
            {
                Utils.BreakIfDebug();
                objReturn = strExpression;
                blnIsSuccess = false;
            }
            catch (XPathException)
            {
                Utils.BreakIfDebug();
                objReturn = strExpression;
                blnIsSuccess = false;
            }
            s_DicCompiledEvaluations.TryAdd(strExpression, new Tuple<bool, object>(blnIsSuccess, objReturn)); // don't want to store managed objects, only primitives
            return objReturn;
        }

        /// <summary>
        /// Parse an XPath for whether it is valid XPath.
        /// </summary>
        /// <param name="strXPathExpression" >XPath Expression to evaluate</param>
        /// <param name="blnIsNullSuccess"   >Should a null or empty result be treated as success?</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCharacterAttributeXPathValidOrNull(string strXPathExpression, bool blnIsNullSuccess = true)
        {
            if (string.IsNullOrEmpty(strXPathExpression))
                return blnIsNullSuccess;
            foreach (string strCharAttributeName in Backend.Attributes.AttributeSection.AttributeStrings)
            {
                if (!string.IsNullOrEmpty(strXPathExpression))
                    strXPathExpression = strXPathExpression
                                         .Replace('{' + strCharAttributeName + '}', "0")
                                         .Replace('{' + strCharAttributeName + "Unaug}", "0")
                                         .Replace('{' + strCharAttributeName + "Base}", "0");
            }

            if (string.IsNullOrEmpty(strXPathExpression)) return true;
            CommonFunctions.EvaluateInvariantXPath(strXPathExpression, out bool blnSuccess);
            return blnSuccess;
        }

        #endregion XPath Evaluators

        #region Find Functions

        /// <summary>
        /// Locate a piece of Gear within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        public static Gear FindVehicleGear(this IEnumerable<Vehicle> lstVehicles, string strGuid)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            return lstVehicles.FindVehicleGear(strGuid, out Vehicle _, out WeaponAccessory _, out Cyberware _);
        }

        /// <summary>
        /// Locate a piece of Gear by matching on its Weapon ID.
        /// </summary>
        /// <param name="strGuid">InternalId of the Weapon to find.</param>
        /// <param name="lstGear">List of Gear to search.</param>
        public static Drug FindDrug(string strGuid, IEnumerable<Drug> lstGear)
        {
            if (lstGear == null)
                throw new ArgumentNullException(nameof(lstGear));
            foreach (Drug objDrug in lstGear)
            {
                if (objDrug.InternalId == strGuid)
                    return objDrug;
            }

            return null;
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        /// <param name="objFoundVehicle">Vehicle that the Gear was found in.</param>
        /// <param name="objFoundWeaponAccessory">Weapon Accessory that the Gear was found in.</param>
        /// <param name="objFoundCyberware">Cyberware that the Gear was found in.</param>
        public static Gear FindVehicleGear(this IEnumerable<Vehicle> lstVehicles, string strGuid, out Vehicle objFoundVehicle, out WeaponAccessory objFoundWeaponAccessory, out Cyberware objFoundCyberware)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            if (!string.IsNullOrEmpty(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Vehicle objVehicle in lstVehicles)
                {
                    Gear objReturn = objVehicle.GearChildren.DeepFindById(strGuid);
                    if (!string.IsNullOrEmpty(objReturn?.Name))
                    {
                        objFoundVehicle = objVehicle;
                        objFoundWeaponAccessory = null;
                        objFoundCyberware = null;
                        return objReturn;
                    }

                    // Look for any Gear that might be attached to this Vehicle through Weapon Accessories or Cyberware.
                    foreach (VehicleMod objMod in objVehicle.Mods)
                    {
                        // Weapon Accessories.
                        objReturn = objMod.Weapons.FindWeaponGear(strGuid, out WeaponAccessory objAccessory);

                        if (!string.IsNullOrEmpty(objReturn?.Name))
                        {
                            objFoundVehicle = objVehicle;
                            objFoundWeaponAccessory = objAccessory;
                            objFoundCyberware = null;
                            return objReturn;
                        }

                        // Cyberware.
                        objReturn = objMod.Cyberware.FindCyberwareGear(strGuid, out Cyberware objCyberware);

                        if (!string.IsNullOrEmpty(objReturn?.Name))
                        {
                            objFoundVehicle = objVehicle;
                            objFoundWeaponAccessory = null;
                            objFoundCyberware = objCyberware;
                            return objReturn;
                        }
                    }
                }
            }

            objFoundVehicle = null;
            objFoundWeaponAccessory = null;
            objFoundCyberware = null;
            return null;
        }

        /// <summary>
        /// Locate a VehicleMod within the character's Vehicles.
        /// </summary>
        /// <param name="funcPredicate">Predicate to locate the VehicleMod.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        public static VehicleMod FindVehicleMod([NotNull] this IEnumerable<Vehicle> lstVehicles, [NotNull] Func<VehicleMod, bool> funcPredicate)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            return lstVehicles.FindVehicleMod(funcPredicate, out Vehicle _, out WeaponMount _);
        }

        /// <summary>
        /// Locate a VehicleMod within the character's Vehicles.
        /// </summary>
        /// <param name="funcPredicate">Predicate to locate the VehicleMod.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        /// <param name="objFoundVehicle">Vehicle that the VehicleMod was found in.</param>
        /// <param name="objFoundWeaponMount">Weapon Mount that the VehicleMod was found in.</param>
        public static VehicleMod FindVehicleMod([NotNull] this IEnumerable<Vehicle> lstVehicles, [NotNull] Func<VehicleMod, bool> funcPredicate, out Vehicle objFoundVehicle, out WeaponMount objFoundWeaponMount)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            foreach (Vehicle objVehicle in lstVehicles)
            {
                VehicleMod objMod = objVehicle.FindVehicleMod(funcPredicate, out objFoundWeaponMount);
                if (objMod != null)
                {
                    objFoundVehicle = objVehicle;
                    return objMod;
                }
            }

            objFoundVehicle = null;
            objFoundWeaponMount = null;
            return null;
        }

        /// <summary>
        /// Locate a Weapon within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">InternalId of the Weapon to find.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        public static Weapon FindVehicleWeapon(this IEnumerable<Vehicle> lstVehicles, string strGuid)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            return lstVehicles.FindVehicleWeapon(strGuid, out Vehicle _, out WeaponMount _, out VehicleMod _);
        }

        /// <summary>
        /// Locate a Weapon within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">InternalId of the Weapon to find.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        /// <param name="objFoundVehicle">Vehicle that the Weapon was found in.</param>
        public static Weapon FindVehicleWeapon(this IEnumerable<Vehicle> lstVehicles, string strGuid, out Vehicle objFoundVehicle)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            return lstVehicles.FindVehicleWeapon(strGuid, out objFoundVehicle, out WeaponMount _, out VehicleMod _);
        }

        /// <summary>
        /// Locate a Weapon within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">InternalId of the Weapon to find.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        /// <param name="objFoundVehicle">Vehicle that the Weapon was found in.</param>
        /// <param name="objFoundVehicleMod">Vehicle mod that the Weapon was found in.</param>
        /// <param name="objFoundWeaponMount">Weapon Mount that the Weapon was found in.</param>
        public static Weapon FindVehicleWeapon(this IEnumerable<Vehicle> lstVehicles, string strGuid, out Vehicle objFoundVehicle, out WeaponMount objFoundWeaponMount, out VehicleMod objFoundVehicleMod)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Vehicle objVehicle in lstVehicles)
                {
                    Weapon objReturn = objVehicle.Weapons.DeepFindById(strGuid);
                    if (objReturn != null)
                    {
                        objFoundVehicle = objVehicle;
                        objFoundWeaponMount = null;
                        objFoundVehicleMod = null;
                        return objReturn;
                    }

                    foreach (WeaponMount objWeaponMount in objVehicle.WeaponMounts)
                    {
                        objReturn = objWeaponMount.Weapons.DeepFindById(strGuid);
                        if (objReturn != null)
                        {
                            objFoundVehicle = objVehicle;
                            objFoundWeaponMount = objWeaponMount;
                            objFoundVehicleMod = null;
                            return objReturn;
                        }

                        foreach (VehicleMod objMod in objWeaponMount.Mods)
                        {
                            objReturn = objMod.Weapons.DeepFindById(strGuid);
                            if (objReturn != null)
                            {
                                objFoundVehicle = objVehicle;
                                objFoundVehicleMod = objMod;
                                objFoundWeaponMount = objWeaponMount;
                                return objReturn;
                            }
                        }
                    }

                    foreach (VehicleMod objMod in objVehicle.Mods)
                    {
                        objReturn = objMod.Weapons.DeepFindById(strGuid);
                        if (objReturn != null)
                        {
                            objFoundVehicle = objVehicle;
                            objFoundVehicleMod = objMod;
                            objFoundWeaponMount = null;
                            return objReturn;
                        }
                    }
                }
            }

            objFoundVehicle = null;
            objFoundWeaponMount = null;
            objFoundVehicleMod = null;
            return null;
        }

        /// <summary>
        /// Locate a Weapon Mount within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">Internal Id with which to look for the vehicle mod.</param>
        /// <param name="lstVehicles">List of root vehicles to search.</param>
        /// <param name="objFoundVehicle">Vehicle in which the Weapon Mount was found.</param>
        /// <returns></returns>
        public static WeaponMount FindVehicleWeaponMount(this IEnumerable<Vehicle> lstVehicles, string strGuid, out Vehicle objFoundVehicle)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Vehicle objVehicle in lstVehicles)
                {
                    foreach (WeaponMount objMod in objVehicle.WeaponMounts)
                    {
                        if (objMod.InternalId == strGuid)
                        {
                            objFoundVehicle = objVehicle;
                            return objMod;
                        }
                    }
                }
            }

            objFoundVehicle = null;
            return null;
        }

        /// <summary>
        /// Locate a Vehicle Mod within the character's Vehicles' weapon mounts.
        /// </summary>
        /// <param name="strGuid">Internal Id with which to look for the vehicle mod.</param>
        /// <param name="lstVehicles">List of root vehicles to search.</param>
        /// <param name="outMount">Weapon Mount in which the Vehicle Mod was found.</param>
        /// <returns></returns>
        public static VehicleMod FindVehicleWeaponMountMod(this IEnumerable<Vehicle> lstVehicles, string strGuid, out WeaponMount outMount)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Vehicle objVehicle in lstVehicles)
                {
                    foreach (WeaponMount objWeaponMount in objVehicle.WeaponMounts)
                    {
                        foreach (VehicleMod objVehicleMod in objWeaponMount.Mods)
                        {
                            if (objVehicleMod.InternalId == strGuid)
                            {
                                outMount = objWeaponMount;
                                return objVehicleMod;
                            }
                        }
                    }
                }
            }

            outMount = null;
            return null;
        }

        /// <summary>
        /// Locate a Weapon Accessory within the character's Vehicles.
        /// </summary>
        /// <param name="strGuid">InternalId of the Weapon Accessory to find.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        public static WeaponAccessory FindVehicleWeaponAccessory(this IEnumerable<Vehicle> lstVehicles, string strGuid)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Vehicle objVehicle in lstVehicles)
                {
                    WeaponAccessory objReturn = objVehicle.Weapons.FindWeaponAccessory(strGuid);
                    if (objReturn != null)
                    {
                        return objReturn;
                    }

                    foreach (WeaponMount objMod in objVehicle.WeaponMounts)
                    {
                        objReturn = objMod.Weapons.FindWeaponAccessory(strGuid);
                        if (objReturn != null)
                        {
                            return objReturn;
                        }
                    }

                    foreach (VehicleMod objMod in objVehicle.Mods)
                    {
                        objReturn = objMod.Weapons.FindWeaponAccessory(strGuid);
                        if (objReturn != null)
                        {
                            return objReturn;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Locate a piece of Cyberware within the character's Vehicles.
        /// </summary>
        /// <param name="funcPredicate">Predicate to locate the Cyberware.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        public static Cyberware FindVehicleCyberware([NotNull] this IEnumerable<Vehicle> lstVehicles, [NotNull] Func<Cyberware, bool> funcPredicate)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            return lstVehicles.FindVehicleCyberware(funcPredicate, out VehicleMod _);
        }

        /// <summary>
        /// Locate a piece of Cyberware within the character's Vehicles.
        /// </summary>
        /// <param name="funcPredicate">Predicate to locate the Cyberware.</param>
        /// <param name="lstVehicles">List of Vehicles to search.</param>
        /// <param name="objFoundVehicleMod">Vehicle Mod to which the Cyberware belongs.</param>
        public static Cyberware FindVehicleCyberware([NotNull] this IEnumerable<Vehicle> lstVehicles, [NotNull] Func<Cyberware, bool> funcPredicate, out VehicleMod objFoundVehicleMod)
        {
            if (lstVehicles == null)
                throw new ArgumentNullException(nameof(lstVehicles));
            foreach (Vehicle objVehicle in lstVehicles)
            {
                Cyberware objReturn = objVehicle.FindVehicleCyberware(funcPredicate, out objFoundVehicleMod);
                if (objReturn != null)
                {
                    return objReturn;
                }
            }

            objFoundVehicleMod = null;
            return null;
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Armors.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstArmors">List of Armors to search.</param>
        public static Gear FindArmorGear(this IEnumerable<Armor> lstArmors, string strGuid)
        {
            if (lstArmors == null)
                throw new ArgumentNullException(nameof(lstArmors));
            return lstArmors.FindArmorGear(strGuid, out Armor _, out ArmorMod _);
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Armors.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstArmors">List of Armors to search.</param>
        /// <param name="objFoundArmor">Armor that the Gear was found in.</param>
        /// <param name="objFoundArmorMod">Armor mod that the Gear was found in.</param>
        public static Gear FindArmorGear(this IEnumerable<Armor> lstArmors, string strGuid, out Armor objFoundArmor, out ArmorMod objFoundArmorMod)
        {
            if (lstArmors == null)
                throw new ArgumentNullException(nameof(lstArmors));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Armor objArmor in lstArmors)
                {
                    Gear objReturn = objArmor.GearChildren.DeepFindById(strGuid);
                    if (objReturn != null)
                    {
                        objFoundArmor = objArmor;
                        objFoundArmorMod = null;
                        return objReturn;
                    }

                    foreach (ArmorMod objMod in objArmor.ArmorMods)
                    {
                        objReturn = objMod.GearChildren.DeepFindById(strGuid);
                        if (objReturn != null)
                        {
                            objFoundArmor = objArmor;
                            objFoundArmorMod = objMod;
                            return objReturn;
                        }
                    }
                }
            }

            objFoundArmor = null;
            objFoundArmorMod = null;
            return null;
        }

        /// <summary>
        /// Locate an Armor Mod within the character's Armors.
        /// </summary>
        /// <param name="strGuid">InternalId of the ArmorMod to Find.</param>
        /// <param name="lstArmors">List of Armors to search.</param>
        public static ArmorMod FindArmorMod(this IEnumerable<Armor> lstArmors, string strGuid)
        {
            if (lstArmors == null)
                throw new ArgumentNullException(nameof(lstArmors));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Armor objArmor in lstArmors)
                {
                    foreach (ArmorMod objMod in objArmor.ArmorMods)
                    {
                        if (objMod.InternalId == strGuid)
                            return objMod;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Cyberware.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstCyberware">List of Cyberware to search.</param>
        public static Gear FindCyberwareGear(this IEnumerable<Cyberware> lstCyberware, string strGuid)
        {
            if (lstCyberware == null)
                throw new ArgumentNullException(nameof(lstCyberware));
            return lstCyberware.FindCyberwareGear(strGuid, out Cyberware _);
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Cyberware.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstCyberware">List of Cyberware to search.</param>
        /// <param name="objFoundCyberware">Cyberware that the Gear was found in.</param>
        public static Gear FindCyberwareGear(this IEnumerable<Cyberware> lstCyberware, string strGuid, out Cyberware objFoundCyberware)
        {
            if (lstCyberware == null)
                throw new ArgumentNullException(nameof(lstCyberware));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Cyberware objCyberware in lstCyberware.DeepWhere(x => x.Children, x => x.GearChildren.Count > 0))
                {
                    Gear objReturn = objCyberware.GearChildren.DeepFindById(strGuid);

                    if (objReturn != null)
                    {
                        objFoundCyberware = objCyberware;
                        return objReturn;
                    }
                }
            }

            objFoundCyberware = null;
            return null;
        }

        /// <summary>
        /// Locate a WeaponAccessory within the character's Weapons.
        /// </summary>
        /// <param name="strGuid">InternalId of the WeaponAccessory to find.</param>
        /// <param name="lstWeapons">List of Weapons to search.</param>
        public static WeaponAccessory FindWeaponAccessory(this IEnumerable<Weapon> lstWeapons, string strGuid)
        {
            if (lstWeapons == null)
                throw new ArgumentNullException(nameof(lstWeapons));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Weapon objWeapon in lstWeapons.DeepWhere(x => x.Children, x => x.WeaponAccessories.Count > 0))
                {
                    foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
                    {
                        if (objAccessory.InternalId == strGuid)
                        {
                            return objAccessory;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Weapons.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstWeapons">List of Weapons to search.</param>
        public static Gear FindWeaponGear(this IEnumerable<Weapon> lstWeapons, string strGuid)
        {
            if (lstWeapons == null)
                throw new ArgumentNullException(nameof(lstWeapons));
            return lstWeapons.FindWeaponGear(strGuid, out WeaponAccessory _);
        }

        /// <summary>
        /// Locate a piece of Gear within the character's Weapons.
        /// </summary>
        /// <param name="strGuid">InternalId of the Gear to find.</param>
        /// <param name="lstWeapons">List of Weapons to search.</param>
        /// <param name="objFoundAccessory">WeaponAccessory that the Gear was found in.</param>
        public static Gear FindWeaponGear(this IEnumerable<Weapon> lstWeapons, string strGuid, out WeaponAccessory objFoundAccessory)
        {
            if (lstWeapons == null)
                throw new ArgumentNullException(nameof(lstWeapons));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Weapon objWeapon in lstWeapons.DeepWhere(x => x.Children, x => x.WeaponAccessories.Any(y => y.GearChildren.Count > 0)))
                {
                    foreach (WeaponAccessory objAccessory in objWeapon.WeaponAccessories)
                    {
                        Gear objReturn = objAccessory.GearChildren.DeepFindById(strGuid);

                        if (objReturn != null)
                        {
                            objFoundAccessory = objAccessory;
                            return objReturn;
                        }
                    }
                }
            }

            objFoundAccessory = null;
            return null;
        }

        /// <summary>
        /// Locate an Enhancement within the character's Enhancements.
        /// </summary>
        /// <param name="strGuid">InternalId of the Art to find.</param>
        /// <param name="objCharacter">The character to search.</param>
        public static Enhancement FindEnhancement(this Character objCharacter, string strGuid)
        {
            if (objCharacter == null)
                throw new ArgumentNullException(nameof(objCharacter));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (Enhancement objEnhancement in objCharacter.Enhancements)
                {
                    if (objEnhancement.InternalId == strGuid)
                        return objEnhancement;
                }

                foreach (Power objPower in objCharacter.Powers)
                {
                    foreach (Enhancement objEnhancement in objPower.Enhancements)
                    {
                        if (objEnhancement.InternalId == strGuid)
                            return objEnhancement;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Locate a Martial Art Technique within the character's Martial Arts.
        /// </summary>
        /// <param name="strGuid">InternalId of the Martial Art Technique to find.</param>
        /// <param name="lstMartialArts">List of Martial Arts to search.</param>
        public static MartialArtTechnique FindMartialArtTechnique(this IEnumerable<MartialArt> lstMartialArts, string strGuid)
        {
            if (lstMartialArts == null)
                throw new ArgumentNullException(nameof(lstMartialArts));
            return lstMartialArts.FindMartialArtTechnique(strGuid, out MartialArt _);
        }

        /// <summary>
        /// Locate a Martial Art Technique within the character's Martial Arts.
        /// </summary>
        /// <param name="strGuid">InternalId of the Martial Art Technique to find.</param>
        /// <param name="lstMartialArts">List of Martial Arts to search.</param>
        /// <param name="objFoundMartialArt">MartialArt the Technique was found in.</param>
        public static MartialArtTechnique FindMartialArtTechnique(this IEnumerable<MartialArt> lstMartialArts, string strGuid, out MartialArt objFoundMartialArt)
        {
            if (lstMartialArts == null)
                throw new ArgumentNullException(nameof(lstMartialArts));
            if (!string.IsNullOrWhiteSpace(strGuid) && !strGuid.IsEmptyGuid())
            {
                foreach (MartialArt objArt in lstMartialArts)
                {
                    foreach (MartialArtTechnique objTechnique in objArt.Techniques)
                    {
                        if (objTechnique.InternalId == strGuid)
                        {
                            objFoundMartialArt = objArt;
                            return objTechnique;
                        }
                    }
                }
            }

            objFoundMartialArt = null;
            return null;
        }

        #endregion Find Functions

        /// <summary>
        /// Book code (using the translated version if applicable).
        /// </summary>
        /// <param name="strAltCode">Book code to search for.</param>
        /// <param name="objCharacter">Character whose custom data to use. If null, will not use any custom data.</param>
        /// <param name="strLanguage">Language to load.</param>
        public static string LanguageBookCodeFromAltCode(string strAltCode, string strLanguage = "", Character objCharacter = null)
        {
            if (!string.IsNullOrWhiteSpace(strAltCode))
            {
                XPathNavigator xmlOriginalCode = XmlManager.LoadXPath("books.xml", objCharacter?.Settings.EnabledCustomDataDirectoryPaths, strLanguage)
                    .SelectSingleNode("/chummer/books/book[altcode = " + strAltCode.CleanXPath() + "]/code");
                return xmlOriginalCode?.Value ?? strAltCode;
            }

            return string.Empty;
        }

        /// <summary>
        /// Book code (using the translated version if applicable).
        /// </summary>
        /// <param name="strCode">Book code to search for.</param>
        /// <param name="objCharacter">Character whose custom data to use. If null, will not use any custom data.</param>
        /// <param name="strLanguage">Language to load.</param>
        public static string LanguageBookShort(string strCode, string strLanguage = "", Character objCharacter = null)
        {
            if (!string.IsNullOrWhiteSpace(strCode))
            {
                XPathNavigator xmlAltCode = XmlManager.LoadXPath("books.xml", objCharacter?.Settings.EnabledCustomDataDirectoryPaths, strLanguage)
                    .SelectSingleNode("/chummer/books/book[code = " + strCode.CleanXPath() + "]/altcode");
                return xmlAltCode?.Value ?? strCode;
            }

            return string.Empty;
        }

        /// <summary>
        /// Book name (using the translated version if applicable).
        /// </summary>
        /// <param name="strCode">Book code to search for.</param>
        /// <param name="objCharacter">Character whose custom data to use. If null, will not use any custom data.</param>
        /// <param name="strLanguage">Language to load.</param>
        public static string LanguageBookLong(string strCode, string strLanguage = "", Character objCharacter = null)
        {
            if (!string.IsNullOrWhiteSpace(strCode))
            {
                XPathNavigator xmlBook = XmlManager.LoadXPath("books.xml", objCharacter?.Settings.EnabledCustomDataDirectoryPaths, strLanguage)
                    .SelectSingleNode("/chummer/books/book[code = " + strCode.CleanXPath() + "]");
                if (xmlBook != null)
                {
                    string strReturn = xmlBook.SelectSingleNode("translate")?.Value ?? xmlBook.SelectSingleNode("name")?.Value;
                    if (!string.IsNullOrWhiteSpace(strReturn))
                        return strReturn;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns an XPath Expression's string that searches an item's name for a string.
        /// </summary>
        /// <param name="strNeedle">String to look for</param>
        /// <param name="strNameElement">Name of the element that corresponds to the item's untranslated name.</param>
        /// <param name="strTranslateElement">Name of the element that corresponds to the item's translated name.</param>
        /// <returns></returns>
        public static string GenerateSearchXPath(string strNeedle, string strNameElement = "name", string strTranslateElement = "translate")
        {
            if (string.IsNullOrEmpty(strNeedle))
                return string.Empty;
            string strSearchText = strNeedle.CleanXPath().ToUpperInvariant();
            // Construct a second needle for French where we have zero-width spaces between a starting consonant and an apostrophe in order to fix ListView's weird way of alphabetically sorting names
            string strSearchText2 = string.Empty;
            if (GlobalSettings.Language.ToUpperInvariant().StartsWith("FR") && strSearchText.Contains('\''))
            {
                strSearchText2 = strSearchText
                                 .Replace("D\'A", "D\u200B\'A")
                                 .Replace("D\'À", "D\u200B\'À")
                                 .Replace("D\'Â", "D\u200B\'Â")
                                 .Replace("D\'E", "D\u200B\'E")
                                 .Replace("D\'É", "D\u200B\'É")
                                 .Replace("D\'È", "D\u200B\'È")
                                 .Replace("D\'Ê", "D\u200B\'Ê")
                                 .Replace("D\'I", "D\u200B\'I")
                                 .Replace("D\'Î", "D\u200B\'Î")
                                 .Replace("D\'Ï", "D\u200B\'Ï")
                                 .Replace("D\'O", "D\u200B\'O")
                                 .Replace("D\'Ô", "D\u200B\'Ô")
                                 .Replace("D\'Œ", "D\u200B\'Œ")
                                 .Replace("D\'U", "D\u200B\'U")
                                 .Replace("D\'Û", "D\u200B\'Û")
                                 .Replace("L\'A", "L\u200B\'A")
                                 .Replace("L\'À", "L\u200B\'À")
                                 .Replace("L\'Â", "L\u200B\'Â")
                                 .Replace("L\'E", "L\u200B\'E")
                                 .Replace("L\'É", "L\u200B\'É")
                                 .Replace("L\'È", "L\u200B\'È")
                                 .Replace("L\'Ê", "L\u200B\'Ê")
                                 .Replace("L\'I", "L\u200B\'I")
                                 .Replace("L\'Î", "L\u200B\'Î")
                                 .Replace("L\'Ï", "L\u200B\'Ï")
                                 .Replace("L\'O", "L\u200B\'O")
                                 .Replace("L\'Ô", "L\u200B\'Ô")
                                 .Replace("L\'Œ", "L\u200B\'Œ")
                                 .Replace("L\'U", "L\u200B\'U")
                                 .Replace("L\'Û", "L\u200B\'Û");
            }

            // Treat everything as being uppercase so the search is case-insensitive.
            string strReturn = string.Format(
                GlobalSettings.InvariantCultureInfo,
                "((not({0}) and contains(translate({1},'abcdefghijklmnopqrstuvwxyzàáâãäåæăąāçčćđďèéêëěęēėģğıìíîïīįķłĺļñňńņòóôõöőøœřŕšśşțťùúûüűůūųẃẁŵẅýỳŷÿžźżß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÆĂĄĀÇČĆĐĎÈÉÊËĚĘĒĖĢĞIÌÍÎÏĪĮĶŁĹĻÑŇŃŅÒÓÔÕÖŐØŒŘŔŠŚŞȚŤÙÚÛÜŰŮŪŲẂẀŴẄÝỲŶŸŽŹŻß'), {2})) " +
                "or contains(translate({0},'abcdefghijklmnopqrstuvwxyzàáâãäåæăąāçčćđďèéêëěęēėģğıìíîïīįķłĺļñňńņòóôõöőøœřŕšśşțťùúûüűůūųẃẁŵẅýỳŷÿžźżß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÆĂĄĀÇČĆĐĎÈÉÊËĚĘĒĖĢĞIÌÍÎÏĪĮĶŁĹĻÑŇŃŅÒÓÔÕÖŐØŒŘŔŠŚŞȚŤÙÚÛÜŰŮŪŲẂẀŴẄÝỲŶŸŽŹŻß'), {2}))",
                strTranslateElement,
                strNameElement,
                strSearchText);
            if (!string.IsNullOrEmpty(strSearchText2))
            {
                strReturn = '(' + strReturn + string.Format(
                    GlobalSettings.InvariantCultureInfo,
                    " or ((not({0}) and contains(translate({1},'abcdefghijklmnopqrstuvwxyzàáâãäåæăąāçčćđďèéêëěęēėģğıìíîïīįķłĺļñňńņòóôõöőøœřŕšśşțťùúûüűůūųẃẁŵẅýỳŷÿžźżß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÆĂĄĀÇČĆĐĎÈÉÊËĚĘĒĖĢĞIÌÍÎÏĪĮĶŁĹĻÑŇŃŅÒÓÔÕÖŐØŒŘŔŠŚŞȚŤÙÚÛÜŰŮŪŲẂẀŴẄÝỲŶŸŽŹŻß'), {2})) " +
                    "or contains(translate({0},'abcdefghijklmnopqrstuvwxyzàáâãäåæăąāçčćđďèéêëěęēėģğıìíîïīįķłĺļñňńņòóôõöőøœřŕšśşțťùúûüűůūųẃẁŵẅýỳŷÿžźżß','ABCDEFGHIJKLMNOPQRSTUVWXYZÀÁÂÃÄÅÆĂĄĀÇČĆĐĎÈÉÊËĚĘĒĖĢĞIÌÍÎÏĪĮĶŁĹĻÑŇŃŅÒÓÔÕÖŐØŒŘŔŠŚŞȚŤÙÚÛÜŰŮŪŲẂẀŴẄÝỲŶŸŽŹŻß'), {2})))",
                    strTranslateElement,
                    strNameElement,
                    strSearchText2);
            }

            return strReturn;
        }

        /// <summary>
        /// Convert Force, 1D6, or 2D6 into a usable value.
        /// </summary>
        /// <param name="strIn">Expression to convert.</param>
        /// <param name="intForce">Force value to use.</param>
        /// <param name="intOffset">Dice offset.</param>
        /// <param name="intMinValueFromForce">Minimum value to return if Force is present (greater than 0).</param>
        /// <returns></returns>
        public static int ExpressionToInt(string strIn, int intForce = 0, int intOffset = 0, int intMinValueFromForce = 1)
        {
            if (string.IsNullOrWhiteSpace(strIn))
                return intOffset;
            int intValue = 1;
            string strForce = intForce.ToString(GlobalSettings.InvariantCultureInfo);
            // This statement is wrapped in a try/catch since trying 1 div 2 results in an error with XSLT.
            try
            {
                object objProcess = EvaluateInvariantXPath(
                    strIn.Replace("/", " div ").Replace("F", strForce).Replace("1D6", strForce)
                         .Replace("2D6", strForce), out bool blnIsSuccess);
                if (blnIsSuccess)
                    intValue = ((double)objProcess).StandardRound();
            }
            catch (OverflowException)
            {
                // Result is text and not a double
            }
            catch (InvalidCastException)
            {
                // swallow this
            }

            intValue += intOffset;
            if (intForce > 0)
            {
                if (intValue < intMinValueFromForce)
                    return intMinValueFromForce;
            }
            else if (intValue < 0)
                return 0;

            return intValue;
        }

        /// <summary>
        /// Convert Force, 1D6, or 2D6 into a usable value.
        /// </summary>
        /// <param name="strIn">Expression to convert.</param>
        /// <param name="intForce">Force value to use.</param>
        /// <param name="decOffset">Dice offset.</param>
        /// <param name="decMinValueFromForce">Minimum value to return if Force is present (greater than 0).</param>
        /// <returns></returns>
        public static decimal ExpressionToDecimal(string strIn, int intForce = 0, decimal decOffset = 0, decimal decMinValueFromForce = 1.0m)
        {
            if (string.IsNullOrWhiteSpace(strIn))
                return decOffset;
            decimal decValue = 1;
            string strForce = intForce.ToString(GlobalSettings.InvariantCultureInfo);
            // This statement is wrapped in a try/catch since trying 1 div 2 results in an error with XSLT.
            try
            {
                object objProcess = EvaluateInvariantXPath(
                    strIn.Replace("/", " div ").Replace("F", strForce).Replace("1D6", strForce)
                         .Replace("2D6", strForce), out bool blnIsSuccess);
                if (blnIsSuccess)
                    decValue = Convert.ToDecimal((double)objProcess);
            }
            catch (OverflowException)
            {
                // Result is text and not a double
            }
            catch (InvalidCastException)
            {
                // swallow this
            }

            decValue += decOffset;
            if (intForce > 0)
            {
                if (decValue < decMinValueFromForce)
                    return decMinValueFromForce;
            }
            else if (decValue < 0)
                return 0;

            return decValue;
        }

        /// <summary>
        /// Verify that the user wants to delete an item.
        /// </summary>
        public static bool ConfirmDelete(string strMessage)
        {
            return !GlobalSettings.ConfirmDelete ||
                   Program.MainForm.ShowMessageBox(strMessage, LanguageManager.GetString("MessageTitle_Delete"),
                       MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Verify that the user wants to spend their Karma and did not accidentally click the button.
        /// </summary>
        public static bool ConfirmKarmaExpense(string strMessage)
        {
            return !GlobalSettings.ConfirmKarmaExpense ||
                   Program.MainForm.ShowMessageBox(strMessage, LanguageManager.GetString("MessageTitle_ConfirmKarmaExpense"),
                       MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public static XmlDocument GenerateCharactersExportXml(CultureInfo objCultureInfo, string strLanguage, params Character[] lstCharacters)
        {
            return GenerateCharactersExportXml(objCultureInfo, strLanguage, CancellationToken.None, lstCharacters);
        }

        public static XmlDocument GenerateCharactersExportXml(CultureInfo objCultureInfo, string strLanguage, CancellationToken objToken, params Character[] lstCharacters)
        {
            XmlDocument objReturn = new XmlDocument {XmlResolver = null};
            // Write the Character information to a MemoryStream so we don't need to create any files.
            using (MemoryStream objStream = new MemoryStream())
            using (XmlTextWriter objWriter = new XmlTextWriter(objStream, Encoding.UTF8))
            {
                // Begin the document.
                objWriter.WriteStartDocument();

                // </characters>
                objWriter.WriteStartElement("characters");

                foreach (Character objCharacter in lstCharacters)
                {
                    objCharacter.PrintToXmlTextWriter(objWriter, objCultureInfo, strLanguage);
                    if (objToken.IsCancellationRequested)
                        return objReturn;
                }

                // </characters>
                objWriter.WriteEndElement();

                // Finish the document and flush the Writer and Stream.
                objWriter.WriteEndDocument();
                objWriter.Flush();

                if (objToken.IsCancellationRequested)
                    return objReturn;

                // Read the stream.
                objStream.Position = 0;
                using (StreamReader objReader = new StreamReader(objStream, Encoding.UTF8, true))
                using (XmlReader objXmlReader = XmlReader.Create(objReader, GlobalSettings.UnSafeXmlReaderSettings))
                    objReturn.Load(objXmlReader);
            }
            return objReturn;
        }

        #region PDF Functions

        /// <summary>
        /// Opens a PDF file using the provided source information.
        /// </summary>
        /// <param name="sender">Control from which this method was called.</param>
        /// <param name="e">EventArgs used when this method was called.</param>
        public static void OpenPdfFromControl(object sender, EventArgs e)
        {
            if (sender is Control objControl)
            {
                Control objLoopControl = objControl;
                Character objCharacter = null;
                while (objLoopControl != null)
                {
                    if (objLoopControl is CharacterShared objShared)
                    {
                        objCharacter = objShared.CharacterObject;
                        break;
                    }

                    objLoopControl = objLoopControl.Parent;
                }

                OpenPdf(objControl.Text, objCharacter, string.Empty, string.Empty, true);
            }
        }

        /// <summary>
        /// Opens a PDF file using the provided source information.
        /// </summary>
        /// <param name="strSource">Book code and page number to open.</param>
        /// <param name="objCharacter">Character whose custom data to use. If null, will not use any custom data.</param>
        /// <param name="strPdfParameters">PDF parameters to use. If empty, use GlobalSettings.PdfParameters.</param>
        /// <param name="strPdfAppPath">PDF parameters to use. If empty, use GlobalSettings.PdfAppPath.</param>
        /// <param name="blnOpenOptions">If set to True, the user will be prompted whether they wish to link a PDF if no PDF is found.</param>
        public static void OpenPdf(string strSource, Character objCharacter = null, string strPdfParameters = "", string strPdfAppPath = "", bool blnOpenOptions = false)
        {
            if (string.IsNullOrEmpty(strSource))
                return;
            if (string.IsNullOrEmpty(strPdfParameters))
                strPdfParameters = GlobalSettings.PdfParameters;
            if (string.IsNullOrEmpty(strPdfAppPath))
                strPdfAppPath = GlobalSettings.PdfAppPath;
            // The user must have specified the arguments of their PDF application in order to use this functionality.
            while (string.IsNullOrWhiteSpace(strPdfParameters) || string.IsNullOrWhiteSpace(strPdfAppPath) || !File.Exists(strPdfAppPath))
            {
                if (!blnOpenOptions || Program.MainForm.ShowMessageBox(LanguageManager.GetString("Message_NoPDFProgramSet"),
                    LanguageManager.GetString("MessageTitle_NoPDFProgramSet"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                using (new CursorWait(Program.MainForm))
                using (frmGlobalSettings frmOptions = new frmGlobalSettings())
                {
                    if (string.IsNullOrWhiteSpace(strPdfAppPath) || !File.Exists(strPdfAppPath))
                        frmOptions.DoLinkPdfReader();
                    if (frmOptions.ShowDialog(Program.MainForm) != DialogResult.OK)
                        return;
                    strPdfParameters = GlobalSettings.PdfParameters;
                    strPdfAppPath = GlobalSettings.PdfAppPath;
                }
            }

            string strSpace = LanguageManager.GetString("String_Space");
            string[] astrSourceParts;
            if (!string.IsNullOrEmpty(strSpace))
                astrSourceParts = strSource.Split(strSpace, StringSplitOptions.RemoveEmptyEntries);
            else if (strSource.StartsWith("SR5", StringComparison.Ordinal))
                astrSourceParts = new[] {"SR5", strSource.Substring(3)};
            else if (strSource.StartsWith("R5", StringComparison.Ordinal))
                astrSourceParts = new[] {"R5", strSource.Substring(2)};
            else
            {
                int i = strSource.Length - 1;
                for (; i >= 0; --i)
                {
                    if (!char.IsNumber(strSource, i))
                    {
                        break;
                    }
                }

                astrSourceParts = new[] {strSource.Substring(0, i), strSource.Substring(i)};
            }

            if (astrSourceParts.Length < 2)
                return;
            if (!int.TryParse(astrSourceParts[1], out int intPage))
                return;

            // Make sure the page is actually a number that we can use as well as being 1 or higher.
            if (intPage < 1)
                return;

            // Revert the sourcebook code to the one from the XML file if necessary.
            string strBook = LanguageBookCodeFromAltCode(astrSourceParts[0], string.Empty, objCharacter);

            // Retrieve the sourcebook information including page offset and PDF application name.
            SourcebookInfo objBookInfo = GlobalSettings.SourcebookInfos.ContainsKey(strBook)
                ? GlobalSettings.SourcebookInfos[strBook]
                : null;
            // If the sourcebook was not found, we can't open anything.
            if (objBookInfo == null)
                return;
            Uri uriPath = null;
            try
            {
                uriPath = new Uri(objBookInfo.Path);
            }
            catch (UriFormatException)
            {
                // Silently swallow the error because PDF fetching is usually done in the background
                objBookInfo.Path = string.Empty;
            }

            // Check if the file actually exists.
            while (uriPath == null || !File.Exists(uriPath.LocalPath))
            {
                if (!blnOpenOptions || Program.MainForm.ShowMessageBox(string.Format(LanguageManager.GetString("Message_NoLinkedPDF"), LanguageBookLong(strBook)),
                        LanguageManager.GetString("MessageTitle_NoLinkedPDF"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                using (new CursorWait(Program.MainForm))
                using (frmGlobalSettings frmOptions = new frmGlobalSettings())
                {
                    frmOptions.DoLinkPdf(objBookInfo.Code);
                    if (frmOptions.ShowDialog(Program.MainForm) != DialogResult.OK)
                        return;
                    uriPath = null;
                    try
                    {
                        uriPath = new Uri(objBookInfo.Path);
                    }
                    catch (UriFormatException)
                    {
                        // Silently swallow the error because PDF fetching is usually done in the background
                        objBookInfo.Path = string.Empty;
                    }
                }
            }

            intPage += objBookInfo.Offset;

            string strParams = strPdfParameters
                               .Replace("{page}", intPage.ToString(GlobalSettings.InvariantCultureInfo))
                               .Replace("{localpath}", uriPath.LocalPath)
                               .Replace("{absolutepath}", uriPath.AbsolutePath);
            ProcessStartInfo objProcess = new ProcessStartInfo
            {
                FileName = strPdfAppPath,
                Arguments = strParams
            };
            objProcess.Start();
        }

        /// <summary>
        /// Gets a textblock from a given PDF document.
        /// </summary>
        /// <param name="strSource">Formatted Source to search, ie SR5 70</param>
        /// <param name="strText">String to search for as an opener</param>
        /// <param name="objCharacter">Character whose custom data to use. If null, will not use any custom data.</param>
        /// <returns></returns>
        public static string GetTextFromPdf(string strSource, string strText, Character objCharacter = null)
        {
            if (string.IsNullOrEmpty(strText) || string.IsNullOrEmpty(strSource))
                return strText;

            string[] strTemp = strSource.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (strTemp.Length < 2)
                return string.Empty;
            if (!int.TryParse(strTemp[1], out int intPage))
                return string.Empty;

            // Make sure the page is actually a number that we can use as well as being 1 or higher.
            if (intPage < 1)
                return string.Empty;

            // Revert the sourcebook code to the one from the XML file if necessary.
            string strBook = LanguageBookCodeFromAltCode(strTemp[0], string.Empty, objCharacter);

            // Retrieve the sourcebook information including page offset and PDF application name.
            SourcebookInfo objBookInfo = GlobalSettings.SourcebookInfos.ContainsKey(strBook)
                ? GlobalSettings.SourcebookInfos[strBook]
                : null;
            // If the sourcebook was not found, we can't open anything.
            if (objBookInfo == null)
                return string.Empty;

            Uri uriPath;
            try
            {
                uriPath = new Uri(objBookInfo.Path);
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }

            // Check if the file actually exists.
            if (!File.Exists(uriPath.LocalPath))
                return string.Empty;
            intPage += objBookInfo.Offset;

            // due to the tag <nameonpage> for the qualities those variants are no longer needed,
            // as such the code would run at most half of the comparisons with the variants
            // but to be sure we find everything still strip unnecessary stuff after the ':' and any number in it.
            // PS: does any qualities have numbers on them? Or is that a chummer thing?
            string strTextToSearch = strText;
            int intPos = strTextToSearch.IndexOf(':');
            if (intPos != -1)
                strTextToSearch = strTextToSearch.Substring(0, intPos);
            strTextToSearch = strTextToSearch.Trim().TrimEndOnce(" I", " II", " III", " IV");

            PdfDocument objPdfDocument = objBookInfo.CachedPdfDocument;
            List<string> lstStringFromPdf = new List<string>(30);
            int intTitleIndex = -1;
            int intBlockEndIndex = -1;
            int intExtraAllCapsInfo = 0;
            bool blnTitleWithColon = false; // it is either an uppercase title or title in a paragraph with a colon
            int intMaxPagesToRead = 3; // parse at most 3 pages of content
            // Loop through each page, starting at the listed page + offset.
            for (; intPage <= objPdfDocument.GetNumberOfPages(); ++intPage)
            {
                // failsafe if something goes wrong, I guess no description takes more than two full pages?
                if (intMaxPagesToRead-- == 0)
                    break;

                int intProcessedStrings = lstStringFromPdf.Count;
                // each page should have its own text extraction strategy for it to work properly
                // this way we don't need to check for previous page appearing in the current page
                // https://stackoverflow.com/questions/35911062/why-are-gettextfrompage-from-itextsharp-returning-longer-and-longer-strings

                string strPageText;
                try
                {
                    strPageText = PdfTextExtractor.GetTextFromPage(objPdfDocument.GetPage(intPage),
                        new SimpleTextExtractionStrategy())
                    .CleanStylisticLigatures().NormalizeWhiteSpace().NormalizeLineEndings();
                }
                catch(IndexOutOfRangeException)
                {
                    return LanguageManager.GetString("Error_Message_PDF_IndexOutOfBounds", false);
                }

                // don't trust it to be correct, trim all whitespace and remove empty strings before we even start
                lstStringFromPdf.AddRange(strPageText.SplitNoAlloc(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)).Select(x => x.Trim()));

                for (int i = intProcessedStrings; i < lstStringFromPdf.Count; i++)
                {
                    // failsafe for languages that don't have case distinction (chinese, japanese, etc)
                    // there not much to be done for those languages, so stop after 10 continuous lines of uppercase text after our title
                    if (intExtraAllCapsInfo > 10)
                        break;

                    string strCurrentLine = lstStringFromPdf[i];
                    // we still haven't found anything
                    if (intTitleIndex == -1)
                    {
                        int intTextToSearchLength = strTextToSearch.Length;
                        int intTitleExtraLines = 0;
                        if (strCurrentLine.Length < intTextToSearchLength)
                        {
                            // if the line is smaller first check if it contains the start of the text, before parsing the rest
                            if (strTextToSearch.StartsWith(strCurrentLine, StringComparison.OrdinalIgnoreCase))
                            {
                                // now just add more lines to it until it is enough
                                StringBuilder sbdCurrentLine = new StringBuilder(strCurrentLine);
                                while (sbdCurrentLine.Length < intTextToSearchLength && (i + intTitleExtraLines + 1) < lstStringFromPdf.Count)
                                {
                                    intTitleExtraLines++;
                                    // add the content plus a space
                                    sbdCurrentLine.Append(' ' + lstStringFromPdf[i + intTitleExtraLines]);
                                }

                                strCurrentLine = sbdCurrentLine.ToString();
                            }
                            else
                            {
                                // just go to the next line
                                continue;
                            }
                        }

                        // now either we have enough text to search or the page doesn't have anymore stuff and must give up
                        if (strCurrentLine.Length < intTextToSearchLength)
                            break;

                        if (strCurrentLine.StartsWith(strTextToSearch, StringComparison.OrdinalIgnoreCase))
                        {
                            // WE FOUND SOMETHING! lets check what kind block we have
                            // if it is bigger it must have a ':' after the name otherwise it is probably the wrong stuff
                            if (strCurrentLine.Length > intTextToSearchLength)
                            {
                                if (strCurrentLine[intTextToSearchLength] == ':')
                                {
                                    intTitleIndex = i;
                                    blnTitleWithColon = true;
                                }
                            }
                            else // if it is not bigger it is the same length
                            {
                                // this must be an upper case title
                                if (strCurrentLine.ToUpperInvariant() == strCurrentLine)
                                {
                                    intTitleIndex = i;
                                    blnTitleWithColon = false;
                                }
                            }

                            // if we found the tile lets finish some things before finding the text block
                            if (intTitleIndex != -1 && intTitleExtraLines > 0)
                            {
                                // if we had to concatenate stuff lets fix the list of strings before continuing
                                lstStringFromPdf[i] = strCurrentLine;
                                lstStringFromPdf.RemoveRange(i + 1, intTitleExtraLines);
                            }
                        }
                    }
                    else // we already found our title, just go to the end of the block
                    {
                        // it is something in all caps we need to verify what it is
                        if (strCurrentLine.ToUpperInvariant() == strCurrentLine)
                        {
                            // if it is header or footer information just remove it
                            // do we also include lines with just numbers as probably page numbers??
                            if (strCurrentLine.All(char.IsDigit) || strCurrentLine.Contains(">>") || strCurrentLine.Contains("<<"))
                            {
                                lstStringFromPdf.RemoveAt(i);
                                // rewind and go again
                                i--;
                                continue;
                            }

                            // if it is a line in all caps following the all caps title just skip it
                            if (!blnTitleWithColon && i == intTitleIndex + intExtraAllCapsInfo + 1)
                            {
                                intExtraAllCapsInfo++;
                                continue;
                            }

                            // if we are here it is the end of the block we found our end, mark it and be done
                            intBlockEndIndex = i;
                            break;
                        }

                        // if it is a title with colon we stop in the next line that has a colon
                        // this is not perfect, if we had bold information we could do more about that
                        if (blnTitleWithColon && strCurrentLine.Contains(':'))
                        {
                            intBlockEndIndex = i;
                            break;
                        }
                    }
                }

                // we scanned the first page and found nothing, just give up
                if (intTitleIndex == -1)
                    return string.Empty;
                // already have our end, quit searching here
                if (intBlockEndIndex != -1)
                    break;
            }

            // we have our textblock, lets format it and be done with it
            if (intBlockEndIndex != -1)
            {
                string[] strArray = lstStringFromPdf.ToArray();
                // if it is a "paragraph title" just concatenate everything
                if (blnTitleWithColon)
                    return string.Join(" ", strArray, intTitleIndex, intBlockEndIndex - intTitleIndex);
                // add the title
                StringBuilder sbdResultContent = new StringBuilder(strArray[intTitleIndex]).AppendLine();
                // if we have extra info add it keeping the line breaks
                if (intExtraAllCapsInfo > 0)
                    sbdResultContent.AppendJoin(Environment.NewLine, strArray, intTitleIndex + 1, intExtraAllCapsInfo).AppendLine();
                int intContentStartIndex = intTitleIndex + intExtraAllCapsInfo + 1;
                // this is the best we can do for now, it will still mangle spell blocks a bit
                for (int i = intContentStartIndex; i < intBlockEndIndex; i++)
                {
                    string strContentString = strArray[i];
                    if (strContentString.Length > 0)
                    {
                        char chrLastChar = strContentString[strContentString.Length - 1];
                        switch (chrLastChar)
                        {
                            case '-':
                                sbdResultContent.Append(strContentString.Substring(0, strContentString.Length - 1));
                                break;
                            // Line ending with a sentence-ending punctuation = line is end of paragraph.
                            // Not fantastic, has plenty of false positives, but simple text extraction strategy cannot
                            // record when a new line starts with a slight indent compared to the previous line (it's a
                            // graphical indent in PDFs, not an actual tab character).
                            case '.':
                            case '?':
                            case '!':
                            case ':':
                            case '。':
                            case '？':
                            case '！':
                            case '：':
                            case '…':
                                sbdResultContent.AppendLine(strContentString);
                                break;

                            default:
                                sbdResultContent.Append(strContentString + ' ');
                                break;
                        }
                    }
                }

                return sbdResultContent.ToString().Trim();
            }

            return string.Empty;
        }

        #endregion PDF Functions

        #region Timescale

        public enum Timescale
        {
            Instant = 0,
            Seconds = 1,
            CombatTurns = 2,
            Minutes = 3,
            Hours = 4,
            Days = 5
        }

        /// <summary>
        /// Convert a string to a Timescale.
        /// </summary>
        /// <param name="strValue">String value to convert.</param>
        public static Timescale ConvertStringToTimescale(string strValue)
        {
            switch (strValue.ToUpperInvariant())
            {
                case "INSTANT":
                case "IMMEDIATE":
                    return Timescale.Instant;

                case "SECOND":
                case "SECONDS":
                    return Timescale.Seconds;

                case "COMBATTURN":
                case "COMBATTURNS":
                    return Timescale.CombatTurns;

                case "MINUTE":
                case "MINUTES":
                    return Timescale.Minutes;

                case "HOUR":
                case "HOURS":
                    return Timescale.Hours;

                case "DAY":
                case "DAYS":
                    return Timescale.Days;

                default:
                    return Timescale.Instant;
            }
        }

        /// <summary>
        /// Convert a string to a Timescale.
        /// </summary>
        /// <param name="strValue">String value to convert.</param>
        /// <param name="blnSingle">Whether to return multiple of the timescale (Hour vs Hours)</param>
        /// <param name="strLanguage">Language to use. If left empty, will use current program language.</param>
        public static string GetTimescaleString(Timescale strValue, bool blnSingle, string strLanguage = "")
        {
            switch (strValue)
            {
                case Timescale.Seconds when blnSingle:
                    return LanguageManager.GetString("String_Second", strLanguage);

                case Timescale.Seconds:
                    return LanguageManager.GetString("String_Seconds", strLanguage);

                case Timescale.CombatTurns when blnSingle:
                    return LanguageManager.GetString("String_CombatTurn", strLanguage);

                case Timescale.CombatTurns:
                    return LanguageManager.GetString("String_CombatTurns", strLanguage);

                case Timescale.Minutes when blnSingle:
                    return LanguageManager.GetString("String_Minute", strLanguage);

                case Timescale.Minutes:
                    return LanguageManager.GetString("String_Minutes", strLanguage);

                case Timescale.Hours when blnSingle:
                    return LanguageManager.GetString("String_Hour", strLanguage);

                case Timescale.Hours:
                    return LanguageManager.GetString("String_Hours", strLanguage);

                case Timescale.Days when blnSingle:
                    return LanguageManager.GetString("String_Day", strLanguage);

                case Timescale.Days:
                    return LanguageManager.GetString("String_Days", strLanguage);

                case Timescale.Instant:
                    return LanguageManager.GetString("String_Immediate", strLanguage);

                default:
                    return LanguageManager.GetString("String_Immediate", strLanguage);
            }
        }

        #endregion Timescale
    }
}
