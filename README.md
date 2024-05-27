The goal of OpenFastDL when it was first developed was to be a fully-community-committed project. 
While obviously there would need to be checks and balances to keep random people from filling it with garbage, the aim was to be a transparent, open, and accessible FastDL host for various gaming communities. 

Using a custom Git LFS server implementation via [Elefess](https://github.com/QuantumToasted/Elefess) allows using Git LFS to double as a public file server, 
meaning a list of files can be viewed both on the public server (https://data.openfast.download/), as well as here on the repository.
The hope is with this approach, that instead of a messy, complicated, or outdated forum-style approach for requesting content to be added/removed, GitHub issues could be used instead.

Eventually, there may come a time where automation is needed to tag, reply to, or close issues, so this project aims to expose the inner workings of that as well, when the time *does* come.

This publicly exposed API source would also allow third-party filehosts to reuse this code (within the limitations of the [license](./LICENSE)) for their own personal file host projects if they so desire to mimic the layout and functionality of OpenFastDL.

**Do note that while the API is defined here, it is by no means accessible by the public and still requires proper authentication both from Git-LFS and any other endpoints.*
