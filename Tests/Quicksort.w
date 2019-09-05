REF 'IO', 'Util' FROM 'Runtime'

Item data = Collect(4, 8, 2, 3, 5, 1, 6, 7, 9, 0)
Item correctData
Repeat 10 [ correctData = correctData.concat(#) ]

If Quicksort(data) = correctData [ Type("Success!") ] [ Type("FAILED") ]

Func Quicksort
[
	If args.num = 0 [ Return ]
	If args.num = 1 [ Return args.0 ]

	Item pivot = args.0

	Item highlist
	Item lowlist

	Repeat args.num - 1
	[
		If args.(# + 1) < pivot
		[ lowlist = lowlist.concat(args.(# + 1)) ]
		[ highlist = highlist.concat(args.(# + 1)) ]
	]

	Return Quicksort(lowlist).Concat(pivot).Concat(Quicksort(highlist))
]