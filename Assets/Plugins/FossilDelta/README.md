Delta compression algorithm for C#
===

[![Build Status](https://secure.travis-ci.org/endel/FossilDelta.svg?branch=master)](https://travis-ci.org/endel/FossilDelta)

> This is a port from the original C implementation. See references below.

Fossil achieves efficient storage and low-bandwidth synchronization through the
use of delta-compression. Instead of storing or transmitting the complete
content of an artifact, fossil stores or transmits only the changes relative to
a related artifact.

* [Format](http://www.fossil-scm.org/index.html/doc/tip/www/delta_format.wiki)
* [Algorithm](http://www.fossil-scm.org/index.html/doc/tip/www/delta_encoder_algorithm.wiki)
* [Original implementation](http://www.fossil-scm.org/index.html/artifact/f3002e96cc35f37b)

Other implementations:

- [JavaScript](https://github.com/dchest/fossil-delta-js) ([Online demo](https://dchest.github.io/fossil-delta-js/))

Installation
---

### NuGet Gallery

FossilDelta is available on the [NuGet Gallery](https://www.nuget.org/packages).

- [NuGet Gallery: FossilDelta](https://www.nuget.org/packages/FossilDelta)

You can add FossilDelta to your project with the **NuGet Package Manager**, by using the following command in the **Package Manager Console**.

    PM> Install-Package FossilDelta

Usage
---

### Fossil.Delta.Create(byte[] origin, byte[] target)

Returns the difference between `origin` and `target` as a byte array (`byte[]`)

### Fossil.Delta.Apply(byte[] origin, byte[] delta)

Apply the `delta` patch on `origin`, returning the final value as byte array
(`byte[]`).

Throws an error if it fails to apply the delta
(e.g. if it was corrupted).

### Fossil.Delta.OutputSize(byte[] delta)

Returns a size of target for this delta.

Throws an error if it can't read the size from delta.
