# Muddlr - a silly Webfinger project

This project exists because, like every other nerd in the world in late 2022, I started experimenting with spending time in ActivityPub-land and saw numerous posts from bloggers about how to make yourself discoverable via Mastodon search by setting up a webfinger endpoint on your website.

One problem was that a lot of the people I wanted to be able to keep track of did not have their own websites for doing so, and I wasn't really interested in the work that seemed to be associated with managing my own instance of Mastodon or other AP systems.

So this silly side project kept me busy for a few nights, building a central registry for searching as though it were a mastodon instance, but it's just the webfinger search part. This way if there's a community disapora, you could setup a single place where everyone can be found regardless of where they go in the fediverse.

The protocol seems like it could support standing up a combo web-finger and link-tree site, so maybe I'll add that down the road.

Like I said, it's a silly project for silly reasons.

### Basic Concept

I didn't want to host a full Mastodon instance, but I wanted a way to search for people when I might now know their Mastodon server or which handle they went with (e.g. if you know me as rumdood, do you search for me as rumdood@someplace.info or dood@someplace.info or matt@someplace.info?). To solve this, Muddlr allows for multiple "locators" for any given account.

This means that you could perform a webfinger query for a user at `someplace.info` using a variety of different account values. You could issue a webfinger request for https://someplace.info/.well-known/webfinger?resource=acct%3Arumdood%40someplace.info or https://someplace.info/.well-known/webfinger?resource=acct%3Adood%40someplace.info and get the same result - but unlike the solutions involving a static webfinger file for an entire site, you could also search for another account.

Additionally, Muddlr supports filtering of links and other information per the WebFinger spec.

### Data storage

Right now the data is all stored as just static JSON files since the data is very likely to almost never change. I took the approach that Mads Kristensen used for his Miniblog project, just using JSON instead of XML. So the finger records are stored as JSON files in the `.data` folder, and all of the files (since I'm not anticipating this holding millions of rows of data) are just cached in-memory. There is a single index file (`account_locators.json`), which contains the various lookups available for any given record.

The initial build used LiteDb for storage as a way to iterate quickly on the data model, but I kept contemplating supporting other databases (especially for eventually adding link-tree building for individual users, which would require user accounts stored somewhere) and decided to defer any database decisions.

### Looking forward

There's really no pressing reason for the API to be an ASP.NET site. This particular project kind of lends itself to a static-site + serverless backend approach, so I've had my eye on migrating the API endtpoints to something like Azure Functions and then putting a static site in front of it for serving the webfinger results and eventually the link-trees.

# As always...

Any PRs or feedback (other than "dude, you suck and you shoudl feel bad") are welcome. Also I guess if you find something useful here, like, let me know so that I can figure out how I didn't see it.