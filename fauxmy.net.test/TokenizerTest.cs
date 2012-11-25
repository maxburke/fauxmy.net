using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using fxmy.net;

namespace fxmy.net.test
{
    class TokenizerTest
    {
        [TestAttribute("TokenizerTest")]
        static bool RunTest()
        {
            using (TextReader reader = new StreamReader(fxmy.net.Log.QUERY_LOG_FILE))
            {
                string line = null;

                /*
                while ((line = reader.ReadLine()) != null)
                {
                    Query query = new Query(line);

                    System.Diagnostics.Debugger.Break();
                }
                 */

                const string testQuery = @"CREATE TABLE wp_users (   ID bigint(20) unsigned NOT NULL auto_increment,   user_login varchar(60) NOT NULL default '',   user_pass varchar(64) NOT NULL default '',   user_nicename varchar(50) NOT NULL default '',   user_email varchar(100) NOT NULL default '',   user_url varchar(100) NOT NULL default '',   user_registered datetime NOT NULL default '0000-00-00 00:00:00',   user_activation_key varchar(60) NOT NULL default '',   user_status int(11) NOT NULL default '0',   display_name varchar(250) NOT NULL default '',   PRIMARY KEY  (ID),   KEY user_login_key (user_login),   KEY user_nicename (user_nicename) ) DEFAULT CHARACTER SET utf8";
                Query query = new Query(testQuery);
                System.Diagnostics.Debugger.Break();

                return true;
            }
        }
    }
}
