Mongo Driver 2.20 possible bug reproduction.

Demonstrating strange behaviour in complex Aggregation with `Unwind` and `Project` stage, which just fill model's fields with default values.

Occures in any version 2.19+, not occured in 2.18 and below (until introducion of LinqProvider.V3). Also not occured with LinqProvider.V2.