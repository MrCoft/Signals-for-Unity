Prefer primary constructors over construction functions, but always store the arguments in private readonly fields
first.
Never use if or any other blocks without braces, even for single-line statements.
Do not commit code. The user will review the code and commit it themselves.
Try to write visually pleasing code. Related stuff should be together. Think of functions as having a bunch of "steps",
eg. separated by spaces. In a class, try thinking of "sections", starting with "private dependencies, constructor, publi
fields, private fields, methods...".
Try to order code so that it tells a story. Imagine explaining it, top to bottom. When writing tests, again, start with
simple stuff at the top, then common stuff together, and weirdest at the end.
