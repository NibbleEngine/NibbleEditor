import shutil
import sys, os, platform
from zipfile import ZipFile
import zipfile

def deploy_windows(conf_name):
    from win32api import GetFileVersionInfo, LOWORD, HIWORD

    def get_version_number (filename):
        info = GetFileVersionInfo (filename, "\\")
        ms = info['FileVersionMS']
        ls = info['FileVersionLS']
        return HIWORD (ms), LOWORD (ms), HIWORD (ls), LOWORD (ls)

    conf_path = os.path.join("../Build", conf_name, "net5.0", "win-x64")
    conf_path = os.path.abspath(conf_path)
    

    if not os.path.exists(conf_path):
        print("Build Path does not exist")
    print("Build Path: ", conf_path)

    lib_path = os.path.join(conf_path, "Lib")
    
    #Copy all dlls from the build path to the Lib folder
    for path in os.listdir(conf_path):
        if (os.path.isdir(path)):
            continue
        if not path.endswith(".dll"):
            continue
        if (path.startswith("Nibble")):
            continue
        print(path)
        source_path = os.path.join(conf_path, path)
        destination_path = os.path.join(lib_path, path)
        shutil.copy2(source_path, destination_path)
        os.remove(source_path)
        
    #Remove deps json
    try:
        os.remove(os.path.join(conf_path, "NibbleEditor.deps.json"))
    except:
        print("Deps json missing")

    #Get assembly version
    exe_version = get_version_number(os.path.join(conf_path, "NibbleEditor.exe"))
    

    #Pack Files to zip
    zipName = "Release_" + "_".join(map(str, exe_version)) + ".zip"
    

    #Write Folders
    zip = ZipFile(os.path.join(conf_path, zipName), 'w')
    
    # for path, directories, files in os.walk(conf_path):
    #     if ("Temp" in directories):
    #         directories.remove("Temp")
        
    #     rel_dir_path = os.path.relpath(path, conf_path)
    #     zfi = zipfile.ZipInfo(rel_dir_path)
    #     zip.write(zfi, '')

    for path, directories, files in os.walk(conf_path):
        if ("Temp" in directories):
            directories.remove("Temp")
        
        for file in files:
            #Skip settings json
            if (file == "settings.json"):
                continue
            if (file == zipName):
                continue
            if (file.endswith(".out")):
                continue
            file_name = os.path.join(path, file)
            rel_file_name = os.path.relpath(file_name, conf_path)
            zip.write(file_name, rel_file_name)
            print("Writing to Zip", file_name, "as", rel_file_name)



if (__name__ == "__main__"):
    
    if (len(sys.argv) < 2):
        print("Usage: python ./deploy.py [CONFIGURATION TO DEPLOY]")
        quit()

    configuration_name = sys.argv[1]
    running_platform = platform.system()    
    
    if (running_platform == "Windows"):
        deploy_windows(configuration_name)
    else:
        print("Not supported Platform")


    
