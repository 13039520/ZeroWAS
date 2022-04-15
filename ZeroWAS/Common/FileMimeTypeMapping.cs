﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ZeroWAS.Common
{
    public static class FileMimeTypeMapping
    {
        private readonly static string mappingText = @"ai=application/postscript,application/pdf
evy=application/envoy
fif=application/fractals
spl=application/futuresplash
hta=application/hta
acx=application/internet-property-stream
hqx=application/mac-binhex40
doc=application/msword
docx=application/vnd.openxmlformats-officedocument.wordprocessingml.document
dot=application/msword
*=application/octet-stream
bin=application/octet-stream
class=application/octet-stream
dms=application/octet-stream
exe=application/octet-stream
lha=application/octet-stream
lzh=application/octet-stream
vsd=application/vnd.visio
oda=application/oda
axs=application/olescript
pdf=application/pdf
prf=application/pics-rules
p10=application/pkcs10
crl=application/pkix-crl
ai=application/postscript,application/pdf
cdr=application/postscript,application/pdf
eps=application/postscript,application/octet-stream
psd=application/postscript,application/octet-stream
rtf=application/rtf
setpay=application/set-payment-initiation
setreg=application/set-registration-initiation
xla=application/vnd.ms-excel
xlc=application/vnd.ms-excel
xlm=application/vnd.ms-excel
xls=application/vnd.ms-excel
xlsx=application/vnd.ms-excel
xlt=application/vnd.ms-excel
xlw=application/vnd.ms-excel
msg=application/vnd.ms-outlook
sst=application/vnd.ms-pkicertstore
cat=application/vnd.ms-pkiseccat
stl=application/vnd.ms-pkistl
pot=application/vnd.ms-powerpoint
pps=application/vnd.ms-powerpoint
ppt=application/vnd.ms-powerpoint
mpp=application/vnd.ms-project
wcm=application/vnd.ms-works
wdb=application/vnd.ms-works
wks=application/vnd.ms-works
wps=application/vnd.ms-works
hlp=application/winhlp
bcpio=application/x-bcpio
cdf=application/x-cdf
z=application/x-compress
tgz=application/x-compressed
cpio=application/x-cpio
csh=application/x-csh
dcr=application/x-director
dir=application/x-director
dxr=application/x-director
dvi=application/x-dvi
gtar=application/x-gtar
gz=application/x-gzip
hdf=application/x-hdf
ins=application/x-internet-signup
isp=application/x-internet-signup
iii=application/x-iphone
js=application/x-javascript
json=application/x-javascript
latex=application/x-latex
mdb=application/x-msaccess
crd=application/x-mscardfile
clp=application/x-msclip
dll=application/x-msdownload
m13=application/x-msmediaview
m14=application/x-msmediaview
mvb=application/x-msmediaview
wmf=application/x-msmetafile
mny=application/x-msmoney
pub=application/x-mspublisher
scd=application/x-msschedule
trm=application/x-msterminal
wri=application/x-mswrite
cdf=application/x-netcdf
nc=application/x-netcdf
pma=application/x-perfmon
pmc=application/x-perfmon
pml=application/x-perfmon
pmr=application/x-perfmon
pmw=application/x-perfmon
p12=application/x-pkcs12
pfx=application/x-pkcs12
p7b=application/x-pkcs7-certificates
spc=application/x-pkcs7-certificates
p7r=application/x-pkcs7-certreqresp
p7c=application/x-pkcs7-mime
p7m=application/x-pkcs7-mime
p7s=application/x-pkcs7-signature
sh=application/x-sh
shar=application/x-shar
swf=application/x-shockwave-flash
sit=application/x-stuffit
sv4cpio=application/x-sv4cpio
sv4crc=application/x-sv4crc
tar=application/x-tar
tcl=application/x-tcl
tex=application/x-tex
texi=application/x-texinfo
texinfo=application/x-texinfo
roff=application/x-troff
t=application/x-troff
tr=application/x-troff
man=application/x-troff-man
me=application/x-troff-me
ms=application/x-troff-ms
ustar=application/x-ustar
src=application/x-wais-source
cer=application/x-x509-ca-cert
crt=application/x-x509-ca-cert
der=application/x-x509-ca-cert
pko=application/ynd.ms-pkipko
zip=application/x-zip-compressed
rar=application/octet-stream
au=audio/basic
snd=audio/basic
mid=audio/mid
rmi=audio/mid
mp3=audio/mpeg
aif=audio/x-aiff
aifc=audio/x-aiff
aiff=audio/x-aiff
m3u=audio/x-mpegurl
ra=audio/x-pn-realaudio
ram=audio/x-pn-realaudio
wav=audio/x-wav
bmp=image/bmp
cod=image/cis-cod
gif=image/gif
ief=image/ief
jpeg=image/jpeg
jpg=image/jpeg
jpe=image/jpeg
png=image/png
jfif=image/pipeg
svg=image/svg+xml
tif=image/tiff
tiff=image/tiff
ras=image/x-cmu-raster
cmx=image/x-cmx
ico=image/x-icon
pnm=image/x-portable-anymap
pbm=image/x-portable-bitmap
pgm=image/x-portable-graymap
ppm=image/x-portable-pixmap
rgb=image/x-rgb
xbm=image/x-xbitmap
xpm=image/x-xpixmap
xwd=image/x-xwindowdump
mht=message/rfc822
mhtml=message/rfc822
nws=message/rfc822
css=text/css
323=text/h323
htm=text/html
html=text/html
stm=text/html
xml=application/xml
uls=text/iuls
bas=text/plain
c=text/plain
h=text/plain
txt=text/plain
csv=text/csv
rtx=text/richtext
sct=text/scriptlet
tsv=text/tab-separated-values
htt=text/webviewhtml
htc=text/x-component
etx=text/x-setext
vcf=text/x-vcard
mp2=video/mpeg
mpa=video/mpeg
mpe=video/mpeg
mpeg=video/mpeg
mpg=video/mpeg
mpv2=video/mpeg
mov=video/quicktime
qt=video/quicktime
lsf=video/x-la-asf
lsx=video/x-la-asf
asf=video/x-ms-asf
asr=video/x-ms-asf
asx=video/x-ms-asf
avi=video/x-msvideo
movie=video/x-sgi-movie
mp4=video/mp4
flv=video/flv
flr=x-world/x-vrml
vrml=x-world/x-vrml
wrl=x-world/x-vrml
wrz=x-world/x-vrml
xaf=x-world/x-vrml
xof=x-world/x-vrml";

        private static Dictionary<string, string> mapping = new Dictionary<string, string>();
        private static bool mappingLoaded = false;
        private static Dictionary<string, string> GetMapping()
        {
            if (mappingLoaded) { return mapping; }
            var mc = System.Text.RegularExpressions.Regex.Matches(mappingText, @"(?<key>[a-z0-9\*]{1,10})=(?<value>[a-z0-9\*/,\-]{3,})");
            foreach (System.Text.RegularExpressions.Match m in mc)
            {
                var key = m.Groups["key"].Value;
                if (mapping.ContainsKey(key))
                {
                    continue;
                }
                mapping.Add(key, m.Groups["value"].Value);
            }
            mappingLoaded = true;
            return mapping;
        }

        public static void AddOrUpdateMapping(string suffix,string mimeType)
        {
            if(string.IsNullOrEmpty(suffix)|| string.IsNullOrEmpty(mimeType)) { return; }
            suffix = suffix.Trim().ToLower();
            mimeType = mimeType.Trim().ToLower();
            var dic = GetMapping();
            if (dic.ContainsKey(suffix))
            {
                dic.Remove(suffix);
            }
            dic.Add(suffix, mimeType);
        }

        public static void LoadFromFile(System.IO.FileInfo file)
        {
            if (file == null || !file.Exists) { return; }
            using (var reader = file.OpenText())
            {
                try
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        if (line.Length > 0 && line.IndexOf("//") != 0)
                        {
                            int index = line.IndexOf('=');
                            if (index > 0 && index + 1 < line.Length)
                            {
                                string suffix = line.Substring(0, index).Trim();
                                suffix = suffix.TrimStart('.');
                                string mime = line.Substring(index + 1).Trim();
                                if (suffix.Length > 0 && mime.Length > 0)
                                {
                                    AddOrUpdateMapping(suffix, mime);
                                }
                            }
                        }
                        line = reader.ReadLine();
                    }
                }
                catch { }
            }
        }
        public static string GetSuffix(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType) || string.IsNullOrEmpty(mimeType)) { return ".unknown"; }
            var dic = GetMapping();
            mimeType = mimeType.Trim().ToLower();
            foreach (string key in dic.Keys)
            {
                string value = dic[key];
                if (value.IndexOf(',') > -1)
                {
                    string[] values = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(string s in values)
                    {
                        if (s == mimeType)
                        {
                            return "." + key;
                        }
                    }
                }
                else
                {
                    if (value == mimeType)
                    {
                        return "." + key;
                    }
                }
            }
            return ".unknown";
        }
        public static string GetMimeType(string suffix)
        {
            if (string.IsNullOrEmpty(suffix) || string.IsNullOrEmpty(suffix)) { return "application/unknown"; }
            suffix = suffix.Replace(".","").Trim().ToLower();
            if (string.IsNullOrEmpty(suffix)) { return "application/unknown"; }
            var dic = GetMapping();
            if (dic.ContainsKey(suffix))
            {
                return dic[suffix];
            }
            return "application/unknown";
        }


    }
}

