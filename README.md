This renode fork is for testing and developing turbo-resin open source firmware for SLA printers. Initially used for mostly development we hope to use it for continuous integration in the future as the project matures.

Directory structure:  
scripts/ - This is where the renode scripts live. There will be at least one per printer (possibly many).  
platforms/ - These will correspond one-to-one with each printer board (possibly each printer board revision).  
cpus/ - These will be extensions on CPUs to account for a specific model. ie: STM32F407 built on the base STM32F4.  

Once renode is installed or compiled add it to your PATH variable.  
Scripts can then be run with:  
`renode scripts/name.resc`
