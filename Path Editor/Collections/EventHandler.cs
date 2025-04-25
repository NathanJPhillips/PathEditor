namespace NobleTech.Products.PathEditor.Collections;

public delegate void EventHandler(object sender);
public delegate void EventHandler<TEventArg1>(object sender, TEventArg1 arg1);
public delegate void EventHandler<TEventArg1, TEventArg2>(object sender, TEventArg1 arg1, TEventArg2 arg2);
public delegate void EventHandler<TEventArg1, TEventArg2, TEventArg3>(object sender, TEventArg1 arg1, TEventArg2 arg2, TEventArg3 arg3);
