using System;
using System.DirectoryServices;

namespace KLCN.CognizeLibrary
{
    public class ActiveDirectoryOperator
    {
        public ActiveDirectoryOperator()
        {
            
        }
        private DirectoryEntry GetDirectoryObject()
        {
            DirectoryEntry entry = null;
            try
            {
                entry = new DirectoryEntry("LDAP://corpfcsint", "ycnspadmin", "fcs@sp937", AuthenticationTypes.Secure);
            }
            catch (Exception ex)
            {
            }
            return entry;
        }
        private DirectoryEntry GetDirectoryEntry(string commonName)
        {
            DirectoryEntry de = GetDirectoryObject();
            DirectorySearcher deSearch = new DirectorySearcher(de);
            deSearch.Filter = "(&(&(objectCategory=person)(objectClass=user))(cn=" + commonName.Replace("\\", "") + "))";
            deSearch.SearchScope = SearchScope.Subtree;
            try
            {
                SearchResult result = deSearch.FindOne();
                de = new DirectoryEntry(result.Path);
                return de;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public string GetDirectoryMobile (string commonName)
        {
            return GetDirectoryEntry(commonName).Properties["Mobile"].Value.ToString();
        }
    }
}
