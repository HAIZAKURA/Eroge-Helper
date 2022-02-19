﻿using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using ErogeHelper.Model.DataServices.Interface;
using ErogeHelper.Model.Repositories.Interface;
using ErogeHelper.Shared;
using ErogeHelper.Shared.Contracts;
using ErogeHelper.Shared.Languages;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace ErogeHelper.ViewModel.HookConfig;

public class HCodeViewModel : ReactiveValidationObject
{
    public Interaction<Unit, string> Show { get; set; } = new();

    public HCodeViewModel(IHookCodeService? hookCodeService = null, IGameDataService? gameDataService = null)
    {
        hookCodeService ??= DependencyResolver.GetService<IHookCodeService>();
        gameDataService ??= DependencyResolver.GetService<IGameDataService>();

        var codeValidation = this.ValidationRule(
            vm => vm.HookCode,
            CodeValidateRegExp,
            Strings.HookPage_HCodeInvalidFormat);

        this.WhenAnyValue(x => x.HookCode,
            code => !string.IsNullOrEmpty(code) && codeValidation.IsValid)
            .ToPropertyEx(this, x => x.CanInsertCode);

        SearchCode = ReactiveCommand.CreateFromObservable(() =>
            hookCodeService.QueryHCode(gameDataService.Md5).Select(g => g?.Games?.Game?.Hook ?? string.Empty));

        SearchCode.Subscribe(x => HookCode = x == string.Empty ? Strings.HookPage_CodeSearchNoResult : x);
    }

    [Reactive]
    public string? HookCode { get; set; }

    [ObservableAsProperty]
    public bool CanInsertCode { get; }

    public ReactiveCommand<Unit, string> SearchCode { get; }

    private bool CodeValidateRegExp(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            // if hcode is null or space, make TextBox normal style
            return true;

        if (code[^1] == ':')
            return false;

        return Regex.IsMatch(code, ConstantValue.CodeRegExp, RegexOptions.Compiled);
    }
}
