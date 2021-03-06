REF 'IO', 'Util' FROM 'Runtime'

{
	Prints out all args.
	If no args are specified, prints "No arguments found!"
}

? Check number of arguments
If Args.Num <= 0
[
	Type("No arguments found!")
]
[
	? For the number of arguments
	Repeat Args.Num
	[
		? Print the argument at # (iteration counter)
		Type(Args.(#))
	]

	Type("Success!")
]