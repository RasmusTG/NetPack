﻿"use strict";

(function (root, factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        //  define(['signalR'], factory); // This causes a problem when requirejs is on page and script tag included rather than require()d.
    } else {
        // Browser globals
        root.publisher = factory(root.signalR);
    }
}(typeof self !== 'undefined' ? self : this, function (signalR) {
    // Use signalR in some fashion.

    // Just return a value to define the module export.
    // This example returns an object, but the module
    // can return a function as the exported value.

    var connection = new signalR.HubConnectionBuilder().withUrl("/hmrhub").build();
    connection.on("FilesChanged", function (filePaths) {
        filePaths.forEach(function (filePath) {
            PubSub.publishSync('FileChanged', filePath);           
            console.log(filePath + " changed!");
        });
    });
    connection.start().catch(function (err) {
        return console.error(err.toString());
    });

    return {};
    }));



