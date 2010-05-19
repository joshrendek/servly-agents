#!/usr/bin/python

# tested on: OSX 10.5.8 Core2Duo Intel Arch  && 10.6 Snow Leapord

#  memory

import urllib2, urllib, socket, os, statvfs, commands
servlyapp = "http://DOMAIN.servly.com/status/update/KEY"

load = os.getloadavg()
disk = os.statvfs(".")

#os info
release_info = os.popen("sw_vers | grep 'ProductVersion:' | grep -o '[0-9]*\.[0-9]*\.[0-9]*").readline()

# kernel info
kernel = os.popen("uname -a").readline()

#procs
procs = os.popen("ps ax | grep -c ''").readline()

#os
opsys = os.popen("uname").readline()

#connections 
conns = int(commands.getoutput("netstat -an | grep -c ':'"))

# bytes
disk_free = (disk.f_bavail * disk.f_frsize)
disk_size = (disk.f_blocks * disk.f_frsize)
disk_used = disk_size - disk_free

# memory usage - EXPECTS BYTES TO BE SENT SO MAKE SURE YOU MULTIPLY BY PROP VALUE
free_memory = 0
used_memory = 0
free = commands.getoutput("top -l 1 | head -n 7 | tail -n 1 | awk '{print $2 \" \" $4 \" \" $6 \" \" $10}'  | sed 's/M//g'").split(' ')
#print free
used_memory = float(free[0]+free[1]+free[2]) * 1024 * 1024
free_memory = float(free[3]) * 1024 * 1024

# get num of cpus
ncpus = int(os.popen("system_profiler | grep 'Number Of Processors: ' | grep -o '[0-9]'").readline())

# cpu usage
# windows: http://kahrn.pastebin.com/f2430d665
# linux: ps -e -o pcpu
usage = 0
cpu_free = os.popen("sar -u 1 | awk '{ print $NF }' | tail -n 1").readline()

# lets get network usage now via sar, we need to figure out which columns are what and sum them
net = commands.getoutput("sar -n DEV 1").split('\n')
# look through first line
find_iface = net[2]

#print find_iface
in_bytes = 0
out_bytes = 0
i = 0
in_bytes_col = 0
out_bytes_col = 0
for h in find_iface.split(' '): #find the position column of the if headers
    if len(h) > 0:
        if h == "Ibytes/s" or h == "rxbyt/s":
           in_bytes_col = i
        if h == "Obytes/s" or h == "txbyt/s":
            out_bytes_col = i
        i+=1
for c in net[3:-1]:
    this_c = c.split(' ')
    x = 0
    for col in this_c:
        if len(col) > 0 and this_c[0] != "Average:":
			try:
				if x == in_bytes_col:
					in_bytes+=float(col)
				if x == out_bytes_col:
					out_bytes+=float(col)
			except ValueError:
				pass
			x+=1


# check for a webserver: nginx, lighttpd, apache (httpd)
web = 0
web_lsws = int(os.popen('ps ax | grep -c "[^/]lshttpd"').readline())-2 # litespeed
web_lhttpd = int(os.popen('ps ax | grep -c "[^/]lighttpd"').readline())-1 # lighttpd
web_nginx = int(os.popen('ps ax | grep -c nginx').readline())-2 #nginx 
web_httpd = int(os.popen('ps ax | grep -c "/httpd" | grep -v "interworx"').readline())-2 #httpd = filter out interworx

print web_httpd
if web_lsws > 0 or web_lhttpd > 0 or web_nginx > 0 or web_httpd > 0:
	web = 1

db = 0
db_mysql = int(os.popen('ps ax | grep -c mysql').readline())-2 # mysql

if db_mysql > 0:
	db = 1

#ftp check
ftp = 0
ftp_proftpd = int(os.popen('ps ax | grep proftpd | grep -v -c grep').readline())

if ftp_proftpd > 0:
	ftp = 1

#ssh check
ssh = 0
sshd = int(os.popen('ps ax | grep sshd | grep -v -c grep').readline())

if sshd > 0:
	ssh = 1

#ssh check
nfs = 0
nfsd = int(os.popen('ps ax | grep nfsd | grep -v -c grep').readline())

if nfsd > 0:
	nfs = 1

#dns check
dns = 0
dns_tinydns = int(os.popen('ps ax | grep tinydns | grep -v -c grep').readline())

if dns_tinydns > 0:
	dns = 1

#mail check
mail = 0
mail_qmail = int(os.popen('ps ax | grep qmail | grep -v -c grep').readline())

if mail_qmail > 0:
	mail = 1

# If you want to track a custom service, uncomment the lines below and do your own checking for whatever service you want
#custom check
custom = 0
#custom_test = int(os.popen('ps ax | grep qmail | grep -v -c grep').readline())
#if mail_qmail > 0
#	mail = 1


ps_output = os.popen('ps -eo user,pid,ppid,rss,vsize,pcpu,pmem,command -O vsize')
ps = ""
for x in ps_output:
	ps += x



login = urllib.urlencode(
    {
        'srvly[cpu_free]': cpu_free,
        'srvly[disk_used]': disk_used,
        'srvly[disk_size]': disk_size,
        'srvly[mem_used]': used_memory,
        'srvly[mem_free]': free_memory,
        'srvly[procs]': procs,
        'srvly[load_one]': "%.2f" % load[0],
        'srvly[load_five]': "%.2f" % load[1],
        'srvly[load_fifteen]': "%.2f" % load[2],
        'srvly[net_in]': in_bytes,
        'srvly[net_out]': out_bytes,
        'srvly[ncpus]': ncpus,
        'srvly[os]': opsys,
        'srvly[web]': web,
        'srvly[db]': db,
        'srvly[connections]': conns,
		'srvly[kernel]': kernel,
		'srvly[release_info]': release_info,
		'srvly[ftp]': ftp,
		'srvly[ssh]': ssh,
		'srvly[nfs]': nfs,
		'srvly[dns]': dns,
		'srvly[mail]': mail,
		'srvly[custom]': custom,
		'srvly[ps]': ps
    }
);
e = urllib2.urlopen(servlyapp, login)
print "Status posted to " + servlyapp
print e.read()

