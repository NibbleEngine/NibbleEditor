import os


for subdir, dirs, files in os.walk("..\Assets"):
    for file in files:
        #print os.path.join(subdir, file)
        filepath = subdir + os.sep + file
        
        #Get file extension
        ext = filepath.split('.')[-1]
        
        if ext.startswith("nb"):
            #Look for NbCore.Math and replace it with NbCore

            # Read in the file
            with open(filepath, 'r') as file:
                filedata = file.read()

            # Replace the target string
            filedata = filedata.replace('LightData', 'NbLightData')
            
            # Write the file out again
            with open(filepath, 'w') as file:
                file.write(filedata)
            
            print (filepath)